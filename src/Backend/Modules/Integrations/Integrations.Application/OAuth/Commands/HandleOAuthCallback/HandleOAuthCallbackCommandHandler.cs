using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.DTOs;
using Integrations.Application.Persistence;
using Integrations.Domain.OAuth;

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

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HandleOAuthCallbackCommandHandler"/>.
    /// </summary>
    public HandleOAuthCallbackCommandHandler(
        IOAuthStateService stateService,
        IAdNetworkAuthService authService,
        IOAuthTokenVaultRepository vaultRepository,
        IOAuthEncryptionService encryptionService,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<OAuthConnectionStatusDto>> Handle(
        HandleOAuthCallbackCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
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

        // 2. Troca de authorization code por tokens com o provedor externo
        var exchangeResult = await _authService.ExchangeCodeAsync(
            statePayload.Platform,
            request.Code,
            request.RedirectUri,
            cancellationToken);

        if (exchangeResult.IsFailure)
        {
            return Result<OAuthConnectionStatusDto>.Failure(exchangeResult.Error);
        }

        var tokens = exchangeResult.Value;

        // 3. Criptografia transparente em repouso com AES-256 (IV aleatório gerado pelo serviço)
        var encryptedAccessToken = _encryptionService.Encrypt(tokens.AccessToken);
        string? encryptedRefreshToken = !string.IsNullOrWhiteSpace(tokens.RefreshToken)
            ? _encryptionService.Encrypt(tokens.RefreshToken)
            : null;

        // 4. Persistência idempotente no Token Vault do workspace
        var existingVault = await _vaultRepository.GetByWorkspaceAndPlatformAsync(
            statePayload.WorkspaceId,
            statePayload.Platform,
            cancellationToken);

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
            targetVault = existingVault;
        }
        else
        {
            var createResult = OAuthTokenVault.Create(
                id: Guid.NewGuid(),
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
