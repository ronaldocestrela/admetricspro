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
    /// <returns>Lista de pontos de dados métricos normalizados.</returns>
    Task<IReadOnlyList<RawMetricPoint>> GetMetricsAsync(
        Guid? workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);
}
