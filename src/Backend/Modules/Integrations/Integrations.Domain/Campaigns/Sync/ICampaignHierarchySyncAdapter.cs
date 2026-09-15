using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Sync;

/// <summary>
/// Contrato para o adaptador de sincronização de hierarquia estrutural com uma plataforma de anúncios.
/// Suporta paginação e extração de contas reais ou sintetizadas.
/// </summary>
public interface ICampaignHierarchySyncAdapter
{
    /// <summary>
    /// Obtém a plataforma atendida pelo adaptador (ex: MetaAds, GoogleAds, TikTokAds, BingAds, Demo).
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Executa a busca paginada da hierarquia completa de campanhas, conjuntos e criativos.
    /// </summary>
    /// <param name="account">Conta de anúncios conectada ao workspace.</param>
    /// <param name="decryptedAccessToken">Token de acesso descriptografado (opcional para contas demo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Hierarquia unificada normalizada ou erro de comunicação/rate limiting.</returns>
    Task<Result<UnifiedCampaignHierarchy>> FetchHierarchyAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default);
}
