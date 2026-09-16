using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Sync;

/// <summary>
/// Contrato para o adaptador de ingestão de métricas analíticas diárias e horárias com uma plataforma de anúncios.
/// </summary>
public interface ICampaignMetricsSyncAdapter
{
    /// <summary>
    /// Obtém a plataforma atendida pelo adaptador (ex: MetaAds, GoogleAds, TikTokAds, BingAds, Demo).
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Executa a busca paginada e consolidada de métricas de desempenho para um período e granularidade.
    /// </summary>
    /// <param name="account">Conta conectada da rede.</param>
    /// <param name="decryptedAccessToken">Token de acesso descriptografado (opcional para contas demo).</param>
    /// <param name="startDateUtc">Data inicial da janela de sincronização.</param>
    /// <param name="endDateUtc">Data final da janela de sincronização.</param>
    /// <param name="granularity">Granularidade temporal solicitada (diária ou horária).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de métricas unificadas ou falha de comunicação/autenticação.</returns>
    Task<Result<IReadOnlyList<UnifiedMetricItem>>> FetchMetricsAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity granularity,
        CancellationToken cancellationToken = default);
}
