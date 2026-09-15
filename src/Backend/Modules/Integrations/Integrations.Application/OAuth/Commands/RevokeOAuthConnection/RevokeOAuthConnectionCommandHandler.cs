using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.OAuth;

namespace Integrations.Application.OAuth.Commands.RevokeOAuthConnection;

/// <summary>
/// Manipulador do comando <see cref="RevokeOAuthConnectionCommand"/>.
/// Notifica a rede externa se suportado, marca a credencial como revogada no cofre e persiste a alteração.
/// </summary>
public sealed class RevokeOAuthConnectionCommandHandler : ICommandHandler<RevokeOAuthConnectionCommand>
{
    private readonly IOAuthTokenVaultRepository _vaultRepository;
    private readonly IAdNetworkAuthService _authService;
    private readonly IOAuthEncryptionService _encryptionService;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RevokeOAuthConnectionCommandHandler"/>.
    /// </summary>
    public RevokeOAuthConnectionCommandHandler(
        IOAuthTokenVaultRepository vaultRepository,
        IAdNetworkAuthService authService,
        IOAuthEncryptionService encryptionService,
        IIntegrationsUnitOfWork unitOfWork)
    {
        _vaultRepository = vaultRepository ?? throw new ArgumentNullException(nameof(vaultRepository));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        RevokeOAuthConnectionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure(
                Error.Validation("RevokeOAuth.EmptyWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Platform) || !OAuthPlatform.IsSupported(request.Platform))
        {
            return Result.Failure(
                Error.Validation("RevokeOAuth.UnsupportedPlatform", $"A plataforma '{request.Platform}' não é suportada."));
        }

        var normalizedPlatform = OAuthPlatform.Normalize(request.Platform);
        var vault = await _vaultRepository.GetByWorkspaceAndPlatformAsync(
            request.WorkspaceId,
            normalizedPlatform,
            cancellationToken);

        if (vault is null)
        {
            return Result.Failure(
                Error.NotFound("RevokeOAuth.NotFound", $"Nenhuma conexão OAuth ativa encontrada para a plataforma '{normalizedPlatform}'."));
        }

        try
        {
            var rawToken = _encryptionService.Decrypt(vault.EncryptedAccessToken);
            await _authService.RevokeTokenAsync(normalizedPlatform, rawToken, cancellationToken);
        }
        catch
        {
            // Revogação no provedor é melhor esforço; a desativação local deve prosseguir
        }

        vault.Revoke();
        _vaultRepository.Update(vault);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
