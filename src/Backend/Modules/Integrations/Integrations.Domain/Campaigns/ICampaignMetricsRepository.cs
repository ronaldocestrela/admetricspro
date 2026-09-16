using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.Campaigns;

/// <summary>
/// Contrato do repositório operacional de métricas analíticas de campanhas,
/// encapsulando a persistência atômica com garantia de idempotência e consultas temporais.
/// </summary>
public interface ICampaignMetricsRepository
{
    /// <summary>
    /// Realiza a inserção ou atualização atômica (upsert) de um lote de métricas analíticas no banco de dados do inquilino.
    /// Garante idempotência absoluta: se um registro com a mesma chave composta de tempo e entidade já existir,
    /// seus valores consolidados são atualizados sem duplicação.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="metrics">Coleção de métricas a serem persistidas.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado contendo a contagem total de registros processados (inseridos ou atualizados).</returns>
    Task<Result<int>> UpsertMetricsBatchAsync(
        Guid workspaceId,
        IReadOnlyList<CampaignMetric> metrics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta métricas analíticas persistidas aplicando filtros por período, granularidade e entidades de campanha.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="startDateUtc">Data inicial de filtro (UTC).</param>
    /// <param name="endDateUtc">Data final de filtro (UTC).</param>
    /// <param name="granularity">Filtro opcional por granularidade (Daily ou Hourly).</param>
    /// <param name="campaignId">Filtro opcional por campanha específica.</param>
    /// <param name="connectedAdAccountId">Filtro opcional por conta conectada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista de métricas correspondentes ordenadas cronologicamente.</returns>
    Task<Result<IReadOnlyList<CampaignMetric>>> GetMetricsAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity? granularity = null,
        Guid? campaignId = null,
        Guid? connectedAdAccountId = null,
        CancellationToken cancellationToken = default);
}
