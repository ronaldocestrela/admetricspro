using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.OAuth;

namespace Integrations.Application.OAuth.Commands.RefreshExpiringTokens;

/// <summary>
/// Manipulador do comando <see cref="RefreshExpiringTokensCommand"/>.
/// Realiza varredura no banco do tenant por tokens próximos ao vencimento,
/// invoca o adaptador correspondente e atualiza os valores cifrados com novo IV.
/// </summary>
public sealed class RefreshExpiringTokensCommandHandler : ICommandHandler<RefreshExpiringTokensCommand, int>
{
    private static readonly TimeSpan DefaultThreshold = TimeSpan.FromHours(72);
    private readonly IOAuthTokenVaultRepository _vaultRepository;
    private readonly IAdNetworkAuthService _authService;
    private readonly IOAuthEncryptionService _encryptionService;
    private readonly IIntegrationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RefreshExpiringTokensCommandHandler"/>.
    /// </summary>
    public RefreshExpiringTokensCommandHandler(
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
    public async Task<Result<int>> Handle(
        RefreshExpiringTokensCommand request,
        CancellationToken cancellationToken)
    {
        var threshold = request.Threshold ?? DefaultThreshold;
        var expiringTokens = await _vaultRepository.GetExpiringTokensAsync(threshold, cancellationToken);

        if (expiringTokens.Count == 0)
        {
            return Result<int>.Success(0);
        }

        var refreshedCount = 0;

        foreach (var vault in expiringTokens)
        {
            try
            {
                // Determina o token base para renovação: refresh_token se existir; caso contrário, access_token atual (ex: Meta)
                var rawTokenToRefresh = !string.IsNullOrWhiteSpace(vault.EncryptedRefreshToken)
                    ? _encryptionService.Decrypt(vault.EncryptedRefreshToken)
                    : _encryptionService.Decrypt(vault.EncryptedAccessToken);

                var refreshResult = await _authService.RefreshTokenAsync(
                    vault.Platform,
                    rawTokenToRefresh,
                    cancellationToken);

                if (refreshResult.IsSuccess)
                {
                    var newTokens = refreshResult.Value;
                    var newEncryptedAccess = _encryptionService.Encrypt(newTokens.AccessToken);
                    string? newEncryptedRefresh = !string.IsNullOrWhiteSpace(newTokens.RefreshToken)
                        ? _encryptionService.Encrypt(newTokens.RefreshToken)
                        : null;

                    vault.UpdateTokens(
                        newEncryptedAccess,
                        newEncryptedRefresh,
                        newTokens.AccessTokenExpiresAtUtc,
                        newTokens.RefreshTokenExpiresAtUtc);

                    _vaultRepository.Update(vault);
                    refreshedCount++;
                }
                else
                {
                    // Se o provedor rejeitou expressamente, marca como expirado
                    vault.MarkExpired();
                    _vaultRepository.Update(vault);
                }
            }
            catch
            {
                // Em caso de falha transitória ou de descriptografia, segue para o próximo token
                vault.MarkExpired();
                _vaultRepository.Update(vault);
            }
        }

        if (refreshedCount > 0 || expiringTokens.Count > 0)
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        return Result<int>.Success(refreshedCount);
    }
}
