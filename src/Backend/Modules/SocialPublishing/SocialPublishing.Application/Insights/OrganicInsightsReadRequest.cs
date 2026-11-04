using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Insights;

/// <summary>
/// Pedido de leitura de métricas orgânicas na rede para um post já publicado.
/// </summary>
/// <param name="AccessToken">Token de acesso já decifrado (nunca persistir).</param>
/// <param name="ExternalAccountId">Identificador da conta publicável na rede (page, ig-user ou open_id).</param>
/// <param name="PublishedExternalId">Identificador externo do post ou mídia publicada.</param>
/// <param name="Platform">Plataforma orgânica de origem.</param>
public sealed record OrganicInsightsReadRequest(
    string AccessToken,
    string ExternalAccountId,
    string PublishedExternalId,
    OrganicPlatform Platform);
