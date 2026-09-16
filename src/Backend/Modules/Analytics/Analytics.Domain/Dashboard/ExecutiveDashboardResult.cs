namespace Analytics.Domain.Dashboard;

/// <summary>
/// Métrica executiva calculada com comparação temporal em relação ao período anterior.
/// </summary>
public sealed record ExecutiveMetricItemResult(
    string MetricKey,
    string Label,
    decimal CurrentValue,
    decimal PreviousValue,
    decimal PercentageChange,
    bool IsPositiveImprovement,
    string UnitFormat);

/// <summary>
/// Ponto na série temporal diária para plotagem de gráficos executivos.
/// </summary>
public sealed record ExecutiveTimeSeriesPointResult(
    DateTime Date,
    decimal Spend,
    decimal Revenue,
    decimal Roas,
    long Clicks,
    long Impressions,
    decimal Conversions);

/// <summary>
/// Proporção de investimento e desempenho por plataforma de anúncios.
/// </summary>
public sealed record PlatformShareResult(
    string Platform,
    decimal Spend,
    decimal Revenue,
    decimal Roas,
    decimal ShareOfSpendPercentage,
    long Clicks,
    decimal Conversions);

/// <summary>
/// Proporção de investimento e desempenho por dispositivo.
/// </summary>
public sealed record DeviceShareResult(
    string Device,
    decimal Spend,
    long Clicks,
    decimal Conversions,
    decimal Roas,
    decimal ShareOfSpendPercentage);

/// <summary>
/// Resultado consolidado do processamento do dashboard executivo no domínio.
/// </summary>
public sealed record ExecutiveDashboardResult(
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    DateTime PreviousStartDateUtc,
    DateTime PreviousEndDateUtc,
    string Currency,
    IReadOnlyList<ExecutiveMetricItemResult> Metrics,
    IReadOnlyList<ExecutiveTimeSeriesPointResult> TimeSeries,
    IReadOnlyList<PlatformShareResult> PlatformBreakdown,
    IReadOnlyList<DeviceShareResult> DeviceBreakdown);
