using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Dashboard;

/// <summary>
/// Contrato do calculador de alta performance para o dashboard executivo analítico cross-network.
/// </summary>
public interface IExecutiveDashboardCalculator
{
    /// <summary>
    /// Calcula os indicadores consolidados do período corrente e anterior, série temporal e participações.
    /// </summary>
    /// <param name="currentPeriodPoints">Pontos de dados analíticos do período corrente.</param>
    /// <param name="previousPeriodPoints">Pontos de dados analíticos do período anterior equivalente.</param>
    /// <param name="currentStartUtc">Data inicial UTC do período corrente.</param>
    /// <param name="currentEndUtc">Data final UTC do período corrente.</param>
    /// <param name="previousStartUtc">Data inicial UTC do período anterior.</param>
    /// <param name="previousEndUtc">Data final UTC do período anterior.</param>
    /// <param name="currency">Moeda de referência (padrão: BRL).</param>
    /// <param name="platformFilter">Filtro opcional por plataforma.</param>
    /// <param name="deviceFilter">Filtro opcional por dispositivo.</param>
    /// <returns>Resultado contendo todos os indicadores calculados e normalizados.</returns>
    Result<ExecutiveDashboardResult> Calculate(
        IEnumerable<RawMetricPoint> currentPeriodPoints,
        IEnumerable<RawMetricPoint> previousPeriodPoints,
        DateTime currentStartUtc,
        DateTime currentEndUtc,
        DateTime previousStartUtc,
        DateTime previousEndUtc,
        string currency = "BRL",
        string? platformFilter = null,
        string? deviceFilter = null);
}
