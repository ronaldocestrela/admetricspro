using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.OAuth.Commands.HandleOAuthCallback;
using Integrations.Application.OAuth.Commands.InitiateOAuthFlow;
using Integrations.Application.OAuth.Commands.RefreshExpiringTokens;
using Integrations.Application.OAuth.Commands.RevokeOAuthConnection;
using Integrations.Application.OAuth.Queries.GetOAuthConnectionsStatus;
using Integrations.Application.Persistence;
using Integrations.Domain.OAuth;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para os handlers CQRS de integração OAuth2.
/// </summary>
public sealed class OAuthCommandHandlerTests
{
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly IOAuthStateService _stateService = Substitute.For<IOAuthStateService>();
    private readonly IAdNetworkAuthService _authService = Substitute.For<IAdNetworkAuthService>();
    private readonly IOAuthTokenVaultRepository _vaultRepository = Substitute.For<IOAuthTokenVaultRepository>();
    private readonly IOAuthEncryptionService _encryptionService = Substitute.For<IOAuthEncryptionService>();
    private readonly IIntegrationsUnitOfWork _unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Construtor configurando mocks básicos.
    /// </summary>
    public OAuthCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(_tenantId);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _encryptionService.Encrypt(Arg.Any<string>()).Returns(ci => $"Encrypted_{ci.Arg<string>()}");
        _encryptionService.Decrypt(Arg.Any<string>()).Returns(ci => ci.Arg<string>().Replace("Encrypted_", ""));
    }

    /// <summary>
    /// Valida que InitiateOAuthFlow gera URL e state com sucesso.
    /// </summary>
    [Fact]
    public async Task InitiateOAuthFlow_ComParametrosValidos_DeveRetornarUrlEState()
    {
        // Arrange
        _stateService.GenerateState(_tenantId, _workspaceId, OAuthPlatform.MetaAds, "https://cb")
            .Returns("state_mock_123");
        _authService.GetAuthorizationUrl(OAuthPlatform.MetaAds, "state_mock_123", "https://cb")
            .Returns(Result<string>.Success("https://facebook.com/auth"));

        var handler = new InitiateOAuthFlowCommandHandler(_tenantContextAccessor, _stateService, _authService);
        var command = new InitiateOAuthFlowCommand(_workspaceId, OAuthPlatform.MetaAds, "https://cb");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AuthorizationUrl.Should().Be("https://facebook.com/auth");
        result.Value.State.Should().Be("state_mock_123");
    }

    /// <summary>
    /// Valida que HandleOAuthCallback cifra tokens e persiste no cofre.
    /// </summary>
    [Fact]
    public async Task HandleOAuthCallback_ComTokensValidos_DeveCriptografarEPersistirNoVault()
    {
        // Arrange
        var statePayload = new OAuthStatePayload(_tenantId, _workspaceId, OAuthPlatform.GoogleAds, "https://cb", DateTime.UtcNow);
        _stateService.ValidateAndUnpackState("valid_state")
            .Returns(Result<OAuthStatePayload>.Success(statePayload));

        var tokens = new OAuthTokenResult("google_raw_access", "google_raw_refresh", DateTime.UtcNow.AddHours(1), null, "ads");
        _authService.ExchangeCodeAsync(OAuthPlatform.GoogleAds, "valid_code", "https://cb", Arg.Any<CancellationToken>())
            .Returns(Result<OAuthTokenResult>.Success(tokens));

        _vaultRepository.GetByWorkspaceAndPlatformAsync(_workspaceId, OAuthPlatform.GoogleAds, Arg.Any<CancellationToken>())
            .Returns((OAuthTokenVault?)null);

        var handler = new HandleOAuthCallbackCommandHandler(
            _stateService,
            _authService,
            _vaultRepository,
            _encryptionService,
            _unitOfWork);

        var command = new HandleOAuthCallbackCommand("valid_code", "valid_state", "https://cb");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Platform.Should().Be(OAuthPlatform.GoogleAds);
        result.Value.Status.Should().Be(OAuthConnectionStatus.Active);

        await _vaultRepository.Received(1).AddAsync(Arg.Is<OAuthTokenVault>(v =>
            v.EncryptedAccessToken == "Encrypted_google_raw_access" &&
            v.EncryptedRefreshToken == "Encrypted_google_raw_refresh"), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que RefreshExpiringTokens renova tokens e atualiza no cofre.
    /// </summary>
    [Fact]
    public async Task RefreshExpiringTokens_ComTokensProximosDaExpiracao_DeveRenovarComProvedor()
    {
        // Arrange
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            OAuthPlatform.TikTokAds,
            "adv_1",
            "TikTok",
            "Encrypted_old_access",
            "Encrypted_old_refresh",
            accessTokenExpiresAtUtc: DateTime.UtcNow.AddHours(10)).Value;

        _vaultRepository.GetExpiringTokensAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new List<OAuthTokenVault> { vault });

        var newTokens = new OAuthTokenResult("new_access", "new_refresh", DateTime.UtcNow.AddDays(1), null, "scopes");
        _authService.RefreshTokenAsync(OAuthPlatform.TikTokAds, "old_refresh", Arg.Any<CancellationToken>())
            .Returns(Result<OAuthTokenResult>.Success(newTokens));

        var handler = new RefreshExpiringTokensCommandHandler(
            _vaultRepository,
            _authService,
            _encryptionService,
            _unitOfWork);

        // Act
        var result = await handler.Handle(new RefreshExpiringTokensCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        vault.EncryptedAccessToken.Should().Be("Encrypted_new_access");
        _vaultRepository.Received(1).Update(vault);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que RevokeOAuthConnection revoga token no provedor e altera status para Revoked.
    /// </summary>
    [Fact]
    public async Task RevokeOAuthConnection_ComConexaoExistente_DeveRevogarEAtualizarStatus()
    {
        // Arrange
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            OAuthPlatform.MetaAds,
            "act_1",
            "Meta",
            "Encrypted_token").Value;

        _vaultRepository.GetByWorkspaceAndPlatformAsync(_workspaceId, OAuthPlatform.MetaAds, Arg.Any<CancellationToken>())
            .Returns(vault);

        var handler = new RevokeOAuthConnectionCommandHandler(
            _vaultRepository,
            _authService,
            _encryptionService,
            _unitOfWork);

        var command = new RevokeOAuthConnectionCommand(_workspaceId, OAuthPlatform.MetaAds);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        vault.Status.Should().Be(OAuthConnectionStatus.Revoked);
        _vaultRepository.Received(1).Update(vault);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que consulta de conexões retorna DTOs com segurança sem vazar tokens cifrados.
    /// </summary>
    [Fact]
    public async Task GetOAuthConnectionsStatus_DeveRetornarMetadadosSeguros()
    {
        // Arrange
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            OAuthPlatform.GoogleAds,
            "cust_1",
            "Conta Google",
            "EncryptedSecret").Value;

        _vaultRepository.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(new List<OAuthTokenVault> { vault });

        var handler = new GetOAuthConnectionsStatusQueryHandler(_vaultRepository);
        var query = new GetOAuthConnectionsStatusQuery(_workspaceId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Platform.Should().Be(OAuthPlatform.GoogleAds);
        result.Value[0].ExternalAccountName.Should().Be("Conta Google");
    }
}
