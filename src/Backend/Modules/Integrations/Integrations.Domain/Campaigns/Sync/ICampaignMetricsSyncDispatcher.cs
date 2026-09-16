using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Sync;

/// <summary>
/// Despachante dinâmico responsável por rotear a ingestão de métricas para o adaptador correspondente
/// com base na plataforma da conta conectada e na flag de demonstração (FTUX).
/// </summary>
public interface ICampaignMetricsSyncDispatcher
{
    /// <summary>
    /// Despacha a requisição de busca de métricas para o adaptador apropriado.
    /// </summary>
    /// <param name="account">Conta de anúncios conectada.</param>
    /// <param name="decryptedAccessToken">Token de acesso descriptografado.</param>
    /// <param name="startDateUtc">Data inicial da janela.</param>
    /// <param name="endDateUtc">Data final da janela.</param>
    /// <param name="granularity">Granularidade temporal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de métricas analíticas normalizadas.</returns>
    Task<Result<IReadOnlyList<UnifiedMetricItem>>> DispatchAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity granularity,
        CancellationToken cancellationToken = default);
}
