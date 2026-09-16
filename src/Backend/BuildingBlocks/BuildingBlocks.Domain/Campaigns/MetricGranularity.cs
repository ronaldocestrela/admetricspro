namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Define a granularidade temporal de consolidação das métricas de desempenho de anúncios.
/// </summary>
public enum MetricGranularity
{
    /// <summary>
    /// Métrica consolidada por dia (resolução de 24 horas).
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Métrica consolidada por hora do dia (0 a 23).
    /// </summary>
    Hourly = 2
}
