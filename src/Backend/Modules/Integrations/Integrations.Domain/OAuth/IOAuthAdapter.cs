using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.OAuth;

/// <summary>
/// Contrato base implementado por cada adaptador especializado de rede de anúncios (Meta, Google, Bing, TikTok).
/// </summary>
public interface IOAuthAdapter
{
    /// <summary>
    /// Identificador da plataforma gerenciada pelo adaptador (<see cref="OAuthPlatform"/>).
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Constrói a URL de redirecionamento para o consent screen da rede com o state anti-CSRF.
    /// </summary>
    /// <param name="state">Token de estado assinado.</param>
    /// <param name="redirectUri">URI de retorno configurada no SaaS.</param>
    /// <returns>URL completa de autorização.</returns>
    string GetAuthorizationUrl(string state, string redirectUri);

    /// <summary>
    /// Troca o authorization code retornado pelo provedor por access token e refresh token.
    /// </summary>
    /// <param name="code">Código de autorização temporal retornado pelo provedor.</param>
    /// <param name="redirectUri">URI de redirecionamento correspondente ao request inicial.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo tokens válidos ou falha de comunicação/autorização.</returns>
    Task<Result<OAuthTokenResult>> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renova preventivamente um access token utilizando o refresh token ou endpoint de extensão (ex: token de 60 dias da Meta).
    /// </summary>
    /// <param name="refreshTokenOrCurrentToken">Refresh token ou token atual a ser estendido.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo os novos tokens ou erro de revogação/expiração.</returns>
    Task<Result<OAuthTokenResult>> RefreshTokenAsync(
        string refreshTokenOrCurrentToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Solicita revogação formal do token junto ao provedor de anúncios externo.
    /// </summary>
    /// <param name="token">Token a ser invalidado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da revogação.</returns>
    Task<Result> RevokeTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}
