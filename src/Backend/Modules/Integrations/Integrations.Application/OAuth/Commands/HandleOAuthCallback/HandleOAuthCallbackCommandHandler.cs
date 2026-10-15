using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.DTOs;
using Integrations.Application.Persistence;
using Integrations.Domain.OAuth;
using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.MultiTenancy;
using MediatR;

namespace Integrations.Application.OAuth.Commands.HandleOAuthCallback;

/// <summary>
/// Manipulador do comando <see cref="HandleOAuthCallbackCommand"/>.
/// Valida o estado anti-CSRF, troca o código por tokens via adaptador de rede,
/// cifra os tokens com AES-256 e persiste no Token Vault de forma atômica.
/// </summary>
public sealed class HandleOAuthCallbackCommandHandler : ICommandHandler<HandleOAuthCallbackCommand, OAuthConnectionStatusDto>
{
    private readonly IOAuthStateService _stateService;
    private readonly IAdNetworkAuthService _authService;
    private readonly IOAuthTokenVaultRepository _vaultRepository;
    private readonly IOAuthEncryptionService _encryptionService;
    private readonly IIntegrationsUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContext;
    private readonly ICurrentUserContext _user;
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HandleOAuthCallbackCommandHandler"/>.
    /// </summary>
    public HandleOAuthCallbackCommandHandler(
        IOAuthStateService stateService,
        IAdNetworkAuthService authService,
        IOAuthTokenVaultRepository vaultRepository,
        IOAuthEncryptionService encryptionService,
        IIntegrationsUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContext,
        ICurrentUserContext user,
        ISender sender)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContext = tenantContext;
        _user = user;
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result<OAuthConnectionStatusDto>> Handle(
        HandleOAuthCallbackCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) && string.IsNullOrEmpty(request.ProviderError))
        {
            return Result<OAuthConnectionStatusDto>.Failure(
                Error.Validation("OAuthCallback.EmptyCode", "O código de autorização é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            return Result<OAuthConnectionStatusDto>.Failure(
                Error.Validation("OAuthCallback.EmptyState", "O parâmetro state é obrigatório."));
        }

        // 1. Validação estrita do token anti-CSRF
        var unpackResult = _stateService.ValidateAndUnpackState(request.State);
        if (unpackResult.IsFailure)
        {
            return Result<OAuthConnectionStatusDto>.Failure(unpackResult.Error);
        }

        var statePayload = unpackResult.Value;

        if (!_user.IsAuthenticated || _user.UserId != statePayload.UserId || statePayload.UserId == Guid.Empty ||
            _tenantContext.TenantContext?.TenantId != statePayload.TenantId ||
            !string.Equals(request.RedirectUri, statePayload.RedirectUri, StringComparison.Ordinal))
            return Result<OAuthConnectionStatusDto>.Failure(Error.Unauthorized("OAuth.ContextMismatch", "O callback não corresponde à sessão que iniciou a autorização."));

        var access = await _sender.Send(new ValidateIntegrationWorkspaceQuery(statePayload.WorkspaceId), cancellationToken);
        if (access.IsFailure) return Result<OAuthConnectionStatusDto>.Failure(access.Error);
        if (!access.Value) return Result<OAuthConnectionStatusDto>.Failure(Error.Forbidden("OAuth.Forbidden", "Sem permissão para gerenciar esta integração."));
        if (!await _vaultRepository.ConsumeStateAsync(request.State, cancellationToken))
            return Result<OAuthConnectionStatusDto>.Failure(Error.Validation("OAuthState.AlreadyUsed", "Autorização expirada ou já utilizada. Inicie uma nova conexão."));
        if (!string.IsNullOrEmpty(request.ProviderError))
            return Result<OAuthConnectionStatusDto>.Failure(Error.Validation("OAuth.Denied", "A autorização foi negada na plataforma."));

        // 2. Troca de authorization code por tokens com o provedor externo
        var exchangeResult = await _authService.ExchangeCodeAsync(
            statePayload.Platform,
            request.Code,
            statePayload.RedirectUri,
            cancellationToken);

        if (exchangeResult.IsFailure)
        {
            return Result<OAuthConnectionStatusDto>.Failure(exchangeResult.Error);
        }

        var tokens = exchangeResult.Value;

        var existingVault = await _vaultRepository.GetByWorkspaceAndPlatformAsync(
            statePayload.WorkspaceId, statePayload.Platform, cancellationToken);
        var connectionId = existingVault?.Id ?? Guid.NewGuid();
        var purpose = statePayload.Platform == OAuthPlatform.MetaAds
            ? OAuthTokenVault.EncryptionPurpose(statePayload.WorkspaceId, connectionId, statePayload.Platform) : null;
        var encryptedAccessToken = _encryptionService.Encrypt(tokens.AccessToken, purpose);
        string? encryptedRefreshToken = !string.IsNullOrWhiteSpace(tokens.RefreshToken)
            ? _encryptionService.Encrypt(tokens.RefreshToken, purpose) : null;

        OAuthTokenVault targetVault;

        if (existingVault != null)
        {
            var updateResult = existingVault.UpdateTokens(
                encryptedAccessToken,
                encryptedRefreshToken,
                tokens.AccessTokenExpiresAtUtc,
                tokens.RefreshTokenExpiresAtUtc);

            if (updateResult.IsFailure)
            {
                return Result<OAuthConnectionStatusDto>.Failure(updateResult.Error);
            }

            _vaultRepository.Update(existingVault);
            existingVault.UpdateAuthorization(tokens.ExternalAccountId, tokens.ExternalAccountName, tokens.Scopes);
            targetVault = existingVault;
        }
        else
        {
            var createResult = OAuthTokenVault.Create(
                id: connectionId,
                workspaceId: statePayload.WorkspaceId,
                platform: statePayload.Platform,
                externalAccountId: tokens.ExternalAccountId ?? "default",
                externalAccountName: tokens.ExternalAccountName ?? statePayload.Platform,
                encryptedAccessToken: encryptedAccessToken,
                encryptedRefreshToken: encryptedRefreshToken,
                accessTokenExpiresAtUtc: tokens.AccessTokenExpiresAtUtc,
                refreshTokenExpiresAtUtc: tokens.RefreshTokenExpiresAtUtc,
                scopes: tokens.Scopes);

            if (createResult.IsFailure)
            {
                return Result<OAuthConnectionStatusDto>.Failure(createResult.Error);
            }

            await _vaultRepository.AddAsync(createResult.Value, cancellationToken);
            targetVault = createResult.Value;
        }

        if (statePayload.Platform == OAuthPlatform.MetaAds)
        {
            targetVault.AwaitAccountSelection();
            var reset = await _sender.Send(new DisconnectMetaAccountsCommand(statePayload.WorkspaceId), cancellationToken);
            if (reset.IsFailure) return Result<OAuthConnectionStatusDto>.Failure(reset.Error);
        }

        // 5. Consolidação atômica da transação
        await _unitOfWork.CommitAsync(cancellationToken);

        var dto = new OAuthConnectionStatusDto(
            targetVault.Id,
            targetVault.WorkspaceId,
            targetVault.Platform,
            targetVault.ExternalAccountId,
            targetVault.ExternalAccountName,
            targetVault.Status,
            targetVault.Scopes,
            targetVault.AccessTokenExpiresAtUtc,
            targetVault.IsExpiringSoon(TimeSpan.FromHours(72)),
            targetVault.CreatedAtUtc,
            targetVault.UpdatedAtUtc);

        return Result<OAuthConnectionStatusDto>.Success(dto);
    }
}
