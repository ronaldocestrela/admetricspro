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
    /// <summary>A chave de escrita é durável e não pode ser consultada por outro workspace.</summary>
    [Fact]
    public async Task MetaOperationJournal_ClaimsOnceAndIsolatesWorkspace()
    {
        var workspaceId = Guid.NewGuid(); var accountId = Guid.NewGuid(); var id = Guid.NewGuid();
        var journal = new global::Integrations.Infrastructure.Meta.MetaOperationRepository(_contextAccessor,
            new global::Integrations.Infrastructure.Persistence.IntegrationsUnitOfWork(_contextAccessor));
        var operation = MetaApiOperation.Create(id, workspaceId, accountId, Guid.NewGuid(), "campaigns", new string('A', 64)).Value;
        (await journal.TryBeginAsync(operation, default)).Value.Should().BeTrue();
        (await journal.TryBeginAsync(operation, default)).Value.Should().BeFalse();
        (await journal.GetAsync(id, Guid.NewGuid(), accountId, default)).Should().BeNull();
        (await journal.GetAsync(id, workspaceId, Guid.NewGuid(), default)).Should().BeNull();
        (await journal.GetAsync(id, workspaceId, accountId, default))!.State.Should().Be("Pending");
    }

    /// <summary>A chave estrangeira composta impede trocar credenciais entre workspaces no banco.</summary>
    [Fact]
    public async Task AccountBinding_CannotReferenceVaultFromAnotherWorkspace()
    {
        var vault = OAuthTokenVault.Create(Guid.NewGuid(), Guid.NewGuid(), "MetaAds", "user", "João", "encrypted").Value;
        var workspaceId = Guid.NewGuid();
        var account = BuildingBlocks.Domain.Tenants.ConnectedAdAccount.Create(Guid.NewGuid(), workspaceId, "MetaAds", "act_11", "Cliente").Value;
        account.BindAuthorization(vault.Id, workspaceId, "Cliente", "BRL", null, 1, "ANALYZE");
        _dbContext.OAuthTokenVaults.Add(vault);
        _dbContext.ConnectedAdAccounts.Add(account);
        var save = () => _dbContext.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>();
    }
    /// <summary>O consumo persiste entre instâncias do repositório e impede replay.</summary>
    [Fact]
    public async Task ConsumeStateAsync_IsAtomicAndRejectsExpiredOrReusedState()
    {
        await _repository.RegisterStateAsync("random-state", DateTime.UtcNow.AddMinutes(10), default);
        await _repository.RegisterStateAsync("expired-state", DateTime.UtcNow.AddMinutes(-1), default);
        await _dbContext.SaveChangesAsync();
        (await _repository.ConsumeStateAsync("random-state", default)).Should().BeTrue();
        (await new OAuthTokenVaultRepository(_contextAccessor).ConsumeStateAsync("random-state", default)).Should().BeFalse();
        (await _repository.ConsumeStateAsync("expired-state", default)).Should().BeFalse();
        (await _repository.ConsumeStateAsync("unknown-state", default)).Should().BeFalse();
    }
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
