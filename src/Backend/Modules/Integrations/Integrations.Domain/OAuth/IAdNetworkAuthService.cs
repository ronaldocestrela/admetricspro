using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.OAuth;

/// <summary>
/// Orquestrador unificado do Hub de Integrações OAuth2 que resolve dinamicamente adaptadores por rede
/// e expõe operações de autorização, troca de código e renovação preventiva.
/// </summary>
public interface IAdNetworkAuthService
{
    /// <summary>
    /// Gera a URL de autorização para a plataforma solicitada.
    /// </summary>
    /// <param name="platform">Plataforma de anúncios alvo (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="state">Token de estado anti-CSRF gerado.</param>
    /// <param name="redirectUri">URI de redirecionamento configurada.</param>
    /// <returns>URL de redirecionamento ou erro de plataforma não suportada.</returns>
    Result<string> GetAuthorizationUrl(string platform, string state, string redirectUri);

    /// <summary>
    /// Troca o authorization code pelo pacote de tokens na rede indicada.
    /// </summary>
    /// <param name="platform">Plataforma de anúncios.</param>
    /// <param name="code">Código de autorização temporal retornado no callback.</param>
    /// <param name="redirectUri">URI de retorno.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo credenciais ou falha de comunicação.</returns>
    Task<Result<OAuthTokenResult>> ExchangeCodeAsync(
        string platform,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa a renovação preventiva do access token utilizando o adaptador correspondente.
    /// </summary>
    /// <param name="platform">Plataforma de anúncios.</param>
    /// <param name="refreshTokenOrCurrentToken">Refresh token ou token atual.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo os novos tokens ou erro de renovação.</returns>
    Task<Result<OAuthTokenResult>> RefreshTokenAsync(
        string platform,
        string refreshTokenOrCurrentToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga as credenciais da integração junto à rede de anúncios.
    /// </summary>
    /// <param name="platform">Plataforma de anúncios.</param>
    /// <param name="token">Token a ser invalidado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da revogação.</returns>
    Task<Result> RevokeTokenAsync(
        string platform,
        string token,
        CancellationToken cancellationToken = default);
}
