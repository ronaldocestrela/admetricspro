using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o repositório operacional <see cref="OAuthTokenVaultRepository"/>.
/// Valida inserção, consulta por workspace/plataforma, detecção de tokens a expirar e deleção.
/// </summary>
public sealed class OAuthTokenVaultRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly OAuthTokenVaultRepository _repository;

    /// <summary>
    /// Inicializa o contexto SQLite em memória e o repositório.
    /// </summary>
    public OAuthTokenVaultRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TenantDbContext(options);
        _dbContext.Database.EnsureCreated();

        _contextAccessor = Substitute.For<ITenantDbContextAccessor>();
        _contextAccessor.GetDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Result<TenantDbContext>.Success(_dbContext));

        _repository = new OAuthTokenVaultRepository(_contextAccessor);
    }

    /// <summary>
    /// Valida que a adição de credenciais ao vault persiste os dados corretamente.
    /// </summary>
    [Fact]
    public async Task AddAsync_E_GetByWorkspaceAndPlatformAsync_DevePersistirERecuperarRegistro()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            workspaceId,
            OAuthPlatform.MetaAds,
            "act_12345",
            "Conta Meta Teste",
            "EncryptedAccessTokenSample",
            "EncryptedRefreshTokenSample",
            DateTime.UtcNow.AddDays(60),
            null,
            "ads_management").Value;

        // Act
        await _repository.AddAsync(vault);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _repository.GetByWorkspaceAndPlatformAsync(workspaceId, OAuthPlatform.MetaAds);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.WorkspaceId.Should().Be(workspaceId);
        retrieved.Platform.Should().Be(OAuthPlatform.MetaAds);
        retrieved.EncryptedAccessToken.Should().Be("EncryptedAccessTokenSample");
        retrieved.Status.Should().Be(OAuthConnectionStatus.Active);
    }

    /// <summary>
    /// Valida busca de conexões expirando dentro de um intervalo pré-determinado.
    /// </summary>
    [Fact]
    public async Task GetExpiringTokensAsync_DeveRetornarApenasTokensProximosDaExpiracao()
    {
        // Arrange
        var ws1 = Guid.NewGuid();
        var ws2 = Guid.NewGuid();

        var expiringSoon = OAuthTokenVault.Create(
            Guid.NewGuid(),
            ws1,
            OAuthPlatform.GoogleAds,
            "cust_1",
            "Google Expirando",
            "Token1",
            accessTokenExpiresAtUtc: DateTime.UtcNow.AddHours(12)).Value;

        var notExpiring = OAuthTokenVault.Create(
            Guid.NewGuid(),
            ws2,
            OAuthPlatform.GoogleAds,
            "cust_2",
            "Google Valido",
            "Token2",
            accessTokenExpiresAtUtc: DateTime.UtcNow.AddDays(30)).Value;

        await _repository.AddAsync(expiringSoon);
        await _repository.AddAsync(notExpiring);
        await _dbContext.SaveChangesAsync();

        // Act
        var expiringList = await _repository.GetExpiringTokensAsync(TimeSpan.FromHours(24));

        // Assert
        expiringList.Should().ContainSingle();
        expiringList[0].WorkspaceId.Should().Be(ws1);
    }

    /// <summary>
    /// Valida remoção de credencial do banco de dados.
    /// </summary>
    [Fact]
    public async Task Remove_DeveExcluirRegistroDoBanco()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            workspaceId,
            OAuthPlatform.TikTokAds,
            "adv_123",
            "TikTok Adv",
            "TokenTikTok").Value;

        await _repository.AddAsync(vault);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(vault);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _repository.GetByWorkspaceAndPlatformAsync(workspaceId, OAuthPlatform.TikTokAds);

        // Assert
        retrieved.Should().BeNull();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
