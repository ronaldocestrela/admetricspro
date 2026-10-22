using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Dashboard;

/// <summary>
/// Provedor de dados analíticos brutos para o dashboard executivo.
/// </summary>
public interface IExecutiveDashboardDataProvider
{
    /// <summary>
    /// Obtém os pontos de métricas brutas no período indicado.
    /// </summary>
    /// <param name="workspaceId">Identificador opcional do workspace.</param>
    /// <param name="startDateUtc">Data inicial UTC.</param>
    /// <param name="endDateUtc">Data final UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <param name="currency">Moeda original esperada para os valores.</param>
    /// <returns>Lista de pontos normalizados ou falha de autorização, leitura ou moeda.</returns>
    Task<Result<IReadOnlyList<RawMetricPoint>>> GetMetricsAsync(
        Guid? workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default,
        string currency = "BRL");
}
