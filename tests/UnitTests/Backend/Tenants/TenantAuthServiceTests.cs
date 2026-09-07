using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Auth.Services;
using Tenants.Infrastructure.Auth;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para a regra de negócio do serviço de autenticação de inquilinos (TenantAuthService).
/// Cobre resolução contextual, validação de status do tenant, credenciais de usuário, integridade de branding e emissão de JWT.
/// </summary>
public sealed class TenantAuthServiceTests : IDisposable
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly ITenantDbContextFactory<TenantDbContext> _tenantDbContextFactory = Substitute.For<ITenantDbContextFactory<TenantDbContext>>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITenantTokenService _tokenService = Substitute.For<ITenantTokenService>();
    private readonly ILogger<TenantAuthService> _logger = Substitute.For<ILogger<TenantAuthService>>();

    private readonly SqliteConnection _sqliteConnection;
    private readonly TenantDbContext _tenantDbContext;

    /// <summary>
    /// Inicializa o contexto in-memory SQLite para testes unitários com isolamento por conexão.
    /// </summary>
    public TenantAuthServiceTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _tenantDbContext = new TenantDbContext(options);
        _tenantDbContext.Database.EnsureCreated();

        _tenantDbContextFactory.CreateDbContextAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantDbContext>.Success(_tenantDbContext));
        _tenantDbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Result<TenantDbContext>.Success(_tenantDbContext));
    }

    /// <summary>
    /// Libera a conexão SQLite in-memory após cada teste.
    /// </summary>
    public void Dispose()
    {
        _tenantDbContext.Dispose();
        _sqliteConnection.Dispose();
    }

    private static Tenant CreateActiveTenant(string subdomain)
    {
        return Tenant.Create(
            "Agência Vanguarda",
            "12345678000190",
            subdomain,
            SubscriptionTier.Starter).Value;
    }

    /// <summary>
    /// Valida que a tentativa de autenticação sem nenhum identificador de tenant falha.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WithoutTenantIdentifier_ShouldReturnIdentifierRequiredError()
    {
        // Arrange
        _tenantContextAccessor.TenantContext.Returns(TenantContext.Empty);
        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.AuthenticateAsync("carlos@vanguarda.com.br", "Senha123!");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.IdentifierRequired");
    }

    /// <summary>
    /// Valida que se o tenant não for encontrado no catálogo Master, retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WhenTenantNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _tenantRepository.GetBySubdomainAsync("inexistente", Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.AuthenticateAsync("carlos@vanguarda.com.br", "Senha123!", "inexistente");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Valida que se o tenant estiver com status Suspenso, o acesso é bloqueado.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WhenTenantIsSuspended_ShouldReturnInactiveError()
    {
        // Arrange
        var tenant = CreateActiveTenant("suspensa");
        tenant.Suspend("Inadimplência financeira");

        _tenantRepository.GetBySubdomainAsync("suspensa", Arg.Any<CancellationToken>())
            .Returns(tenant);

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.AuthenticateAsync("carlos@suspensa.com.br", "Senha123!", "suspensa");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Inactive");
    }

    /// <summary>
    /// Valida que se o usuário não for encontrado no banco do tenant, retorna credenciais inválidas.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WhenUserNotFoundInTenantDb_ShouldReturnInvalidCredentials()
    {
        // Arrange
        var tenant = CreateActiveTenant("vanguarda");
        _tenantRepository.GetBySubdomainAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(tenant);

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.AuthenticateAsync("naoexiste@vanguarda.com.br", "Senha123!", "vanguarda");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    /// <summary>
    /// Valida que se a conta do usuário estiver inativa, retorna erro de conta desativada.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WhenUserIsInactive_ShouldReturnAccountInactiveError()
    {
        // Arrange
        var tenant = CreateActiveTenant("vanguarda");
        _tenantRepository.GetBySubdomainAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(tenant);

        var user = TenantUser.Create(
            Guid.NewGuid(),
            "Carlos Inativo",
            "inativo@vanguarda.com.br",
            null,
            "hash_123",
            TenantRole.Admin).Value;

        user.Deactivate();

        _tenantDbContext.TenantUsers.Add(user);
        await _tenantDbContext.SaveChangesAsync();

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.AuthenticateAsync("inativo@vanguarda.com.br", "Senha123!", "vanguarda");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AccountInactive");
    }

    /// <summary>
    /// Valida que se a senha informada for incorreta, retorna erro de credenciais inválidas.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ShouldReturnInvalidCredentials()
    {
        // Arrange
        var tenant = CreateActiveTenant("vanguarda");
        _tenantRepository.GetBySubdomainAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(tenant);

        var user = TenantUser.Create(
            Guid.NewGuid(),
            "Carlos Gestor",
            "carlos@vanguarda.com.br",
            null,
            "hash_123",
            TenantRole.Owner).Value;

        _tenantDbContext.TenantUsers.Add(user);
        await _tenantDbContext.SaveChangesAsync();

        _passwordHasher.VerifyPassword("hash_123", "SenhaIncorreta").Returns(false);

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.AuthenticateAsync("carlos@vanguarda.com.br", "SenhaIncorreta", "vanguarda");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    /// <summary>
    /// Valida que com credenciais corretas, o token é gerado e o DTO é preenchido com dados do usuário e branding.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnSuccessWithDtoAndToken()
    {
        // Arrange
        var tenant = CreateActiveTenant("vanguarda");
        _tenantRepository.GetBySubdomainAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(tenant);

        var user = TenantUser.Create(
            Guid.NewGuid(),
            "Carlos Gestor",
            "carlos@vanguarda.com.br",
            null,
            "secure_hash",
            TenantRole.Owner).Value;

        var branding = TenantBranding.Create(
            Guid.NewGuid(),
            "#1E40AF",
            "#F59E0B",
            "https://cdn.admetricspro.internal/logos/vanguarda-light.png",
            null,
            null).Value;

        _tenantDbContext.TenantUsers.Add(user);
        _tenantDbContext.TenantBranding.Add(branding);
        await _tenantDbContext.SaveChangesAsync();

        _passwordHasher.VerifyPassword("secure_hash", "SenhaCorreta123!").Returns(true);
        _tokenService.GenerateToken(user, tenant.Id.Value, tenant.Subdomain)
            .Returns(Result<string>.Success("jwt_token_assinado_123"));

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.AuthenticateAsync("carlos@vanguarda.com.br", "SenhaCorreta123!", "vanguarda");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt_token_assinado_123");
        result.Value.Email.Should().Be("carlos@vanguarda.com.br");
        result.Value.FullName.Should().Be("Carlos Gestor");
        result.Value.Role.Should().Be("Owner");
        result.Value.TenantId.Should().Be(tenant.Id.Value);
        result.Value.Subdomain.Should().Be("vanguarda");
        result.Value.Branding.Should().NotBeNull();
        result.Value.Branding!.AgencyName.Should().Be("Agência Vanguarda");
        result.Value.Branding!.PrimaryColor.Should().Be("#1E40AF");
    }

    /// <summary>
    /// Valida que GetPublicBrandingAsync com subdomínio válido e ativo retorna os dados públicos de branding.
    /// </summary>
    [Fact]
    public async Task GetPublicBrandingAsync_WithValidSubdomain_ShouldReturnPublicBranding()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Agência Vanguarda",
            "12345678000195",
            "vanguarda",
            SubscriptionTier.Pro,
            DateTime.UtcNow.AddMonths(3),
            primaryColor: "#1E40AF",
            secondaryColor: "#F59E0B").Value;

        _tenantRepository.GetBySubdomainAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(tenant);

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.GetPublicBrandingAsync("vanguarda");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyName.Should().Be("Agência Vanguarda");
        result.Value.Subdomain.Should().Be("vanguarda");
        result.Value.PrimaryColor.Should().Be("#1E40AF");
        result.Value.SecondaryColor.Should().Be("#F59E0B");
        result.Value.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Valida que GetPublicBrandingAsync com subdomínio inexistente retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task GetPublicBrandingAsync_WithNonExistentSubdomain_ShouldReturnNotFound()
    {
        // Arrange
        _tenantRepository.GetBySubdomainAsync("inexistente", Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.GetPublicBrandingAsync("inexistente");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Valida que GetPublicBrandingAsync com subdomínio suspenso retorna erro Tenant.Inactive.
    /// </summary>
    [Fact]
    public async Task GetPublicBrandingAsync_WithSuspendedTenant_ShouldReturnInactive()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Agência Suspensa",
            "12345678000195",
            "suspensa",
            SubscriptionTier.Pro,
            DateTime.UtcNow.AddMonths(3)).Value;

        tenant.Suspend("Inadimplência");

        _tenantRepository.GetBySubdomainAsync("suspensa", Arg.Any<CancellationToken>())
            .Returns(tenant);

        var sut = new TenantAuthService(
            _tenantRepository,
            _tenantContextAccessor,
            _tenantDbContextFactory,
            _passwordHasher,
            _tokenService,
            _logger);

        // Act
        var result = await sut.GetPublicBrandingAsync("suspensa");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Inactive");
    }
}

