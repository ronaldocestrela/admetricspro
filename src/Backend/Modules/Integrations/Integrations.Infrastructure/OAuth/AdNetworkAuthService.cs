using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.OAuth;

namespace Integrations.Infrastructure.OAuth;

/// <summary>
/// Orquestrador unificado de autenticação OAuth2 para redes de anúncios.
/// Resolve o adaptador apropriado a partir da plataforma informada.
/// </summary>
public sealed class AdNetworkAuthService : IAdNetworkAuthService
{
    private readonly Dictionary<string, IOAuthAdapter> _adapters;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AdNetworkAuthService"/>.
    /// </summary>
    /// <param name="adapters">Coleção de adaptadores injetados.</param>
    public AdNetworkAuthService(IEnumerable<IOAuthAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        _adapters = adapters.ToDictionary(a => a.Platform, a => a, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Result<string> GetAuthorizationUrl(string platform, string state, string redirectUri)
    {
        var adapterResult = ResolveAdapter(platform);
        if (adapterResult.IsFailure)
        {
            return Result<string>.Failure(adapterResult.Error);
        }

        var url = adapterResult.Value.GetAuthorizationUrl(state, redirectUri);
        return Result<string>.Success(url);
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> ExchangeCodeAsync(
        string platform,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var adapterResult = ResolveAdapter(platform);
        if (adapterResult.IsFailure)
        {
            return Result<OAuthTokenResult>.Failure(adapterResult.Error);
        }

        return await adapterResult.Value.ExchangeCodeAsync(code, redirectUri, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> RefreshTokenAsync(
        string platform,
        string refreshTokenOrCurrentToken,
        CancellationToken cancellationToken = default)
    {
        var adapterResult = ResolveAdapter(platform);
        if (adapterResult.IsFailure)
        {
            return Result<OAuthTokenResult>.Failure(adapterResult.Error);
        }

        return await adapterResult.Value.RefreshTokenAsync(refreshTokenOrCurrentToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> RevokeTokenAsync(
        string platform,
        string token,
        CancellationToken cancellationToken = default)
    {
        var adapterResult = ResolveAdapter(platform);
        if (adapterResult.IsFailure)
        {
            return Result.Failure(adapterResult.Error);
        }

        return await adapterResult.Value.RevokeTokenAsync(token, cancellationToken);
    }

    private Result<IOAuthAdapter> ResolveAdapter(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<IOAuthAdapter>.Failure(
                Error.Validation("AdNetworkAuth.InvalidPlatform", "A plataforma de anúncios não pode ser vazia."));
        }

        if (!_adapters.TryGetValue(platform.Trim(), out var adapter))
        {
            return Result<IOAuthAdapter>.Failure(
                Error.Validation("AdNetworkAuth.UnsupportedPlatform", $"A plataforma '{platform}' não possui um adaptador OAuth2 registrado."));
        }

        return Result<IOAuthAdapter>.Success(adapter);
    }
}
