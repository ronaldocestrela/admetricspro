using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Dashboard;

/// <summary>
/// Implementação concreta do calculador executivo analítico (<see cref="IExecutiveDashboardCalculator"/>).
/// Consolida métricas, deltas percentuais com respeito à polaridade invertida de custos, séries temporais e proporções.
/// </summary>
public sealed class ExecutiveDashboardCalculator : IExecutiveDashboardCalculator
{
    /// <inheritdoc />
    public Result<ExecutiveDashboardResult> Calculate(
        IEnumerable<RawMetricPoint> currentPeriodPoints,
        IEnumerable<RawMetricPoint> previousPeriodPoints,
        DateTime currentStartUtc,
        DateTime currentEndUtc,
        DateTime previousStartUtc,
        DateTime previousEndUtc,
        string currency = "BRL",
        string? platformFilter = null,
        string? deviceFilter = null)
    {
        if (currentStartUtc > currentEndUtc)
        {
            return Result<ExecutiveDashboardResult>.Failure(
                Error.Validation("ExecutiveDashboard.InvalidDateRange", "A data inicial não pode ser superior à data final."));
        }

        var currentFiltered = FilterPoints(currentPeriodPoints, platformFilter, deviceFilter);
        var previousFiltered = FilterPoints(previousPeriodPoints, platformFilter, deviceFilter);

        // Agregação dos períodos
        var curSpend = currentFiltered.Sum(p => p.Spend);
        var curRevenue = currentFiltered.Sum(p => p.Revenue);
        var curClicks = currentFiltered.Sum(p => p.Clicks);
        var curImpressions = currentFiltered.Sum(p => p.Impressions);
        var curConversions = currentFiltered.Sum(p => p.Conversions);

        var prevSpend = previousFiltered.Sum(p => p.Spend);
        var prevRevenue = previousFiltered.Sum(p => p.Revenue);
        var prevClicks = previousFiltered.Sum(p => p.Clicks);
        var prevImpressions = previousFiltered.Sum(p => p.Impressions);
        var prevConversions = previousFiltered.Sum(p => p.Conversions);

        // KPIs Período Atual
        var curCpc = curClicks > 0 ? Math.Round(curSpend / curClicks, 2) : 0m;
        var curCpm = curImpressions > 0 ? Math.Round((curSpend / curImpressions) * 1000m, 2) : 0m;
        var curCtr = curImpressions > 0 ? Math.Round(((decimal)curClicks / curImpressions) * 100m, 2) : 0m;
        var curCpa = curConversions > 0 ? Math.Round(curSpend / curConversions, 2) : 0m;
        var curRoas = curSpend > 0 ? Math.Round(curRevenue / curSpend, 2) : 0m;

        // KPIs Período Anterior
        var prevCpc = prevClicks > 0 ? Math.Round(prevSpend / prevClicks, 2) : 0m;
        var prevCpm = prevImpressions > 0 ? Math.Round((prevSpend / prevImpressions) * 1000m, 2) : 0m;
        var prevCtr = prevImpressions > 0 ? Math.Round(((decimal)prevClicks / prevImpressions) * 100m, 2) : 0m;
        var prevCpa = prevConversions > 0 ? Math.Round(prevSpend / prevConversions, 2) : 0m;
        var prevRoas = prevSpend > 0 ? Math.Round(prevRevenue / prevSpend, 2) : 0m;

        // Lista de 6 cartões mandatários: Spend, CPC, CPM, CTR, CPA, ROAS
        var metrics = new List<ExecutiveMetricItemResult>
        {
            BuildMetricItem("Spend", "Investimento Total", curSpend, prevSpend, "Currency", isInvertedCost: false),
            BuildMetricItem("Cpc", "Custo por Clique (CPC)", curCpc, prevCpc, "Currency", isInvertedCost: true),
            BuildMetricItem("Cpm", "Custo por Mil Impressões (CPM)", curCpm, prevCpm, "Currency", isInvertedCost: true),
            BuildMetricItem("Ctr", "Taxa de Cliques (CTR)", curCtr, prevCtr, "Percentage", isInvertedCost: false),
            BuildMetricItem("Cpa", "Custo por Aquisição (CPA)", curCpa, prevCpa, "Currency", isInvertedCost: true),
            BuildMetricItem("Roas", "Retorno sobre Ad Spend (ROAS)", curRoas, prevRoas, "Multiplier", isInvertedCost: false)
        };

        // Série Temporal Diária
        var timeSeries = currentFiltered
            .GroupBy(p => p.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var daySpend = g.Sum(p => p.Spend);
                var dayRevenue = g.Sum(p => p.Revenue);
                var dayRoas = daySpend > 0 ? Math.Round(dayRevenue / daySpend, 2) : 0m;
                return new ExecutiveTimeSeriesPointResult(
                    g.Key,
                    daySpend,
                    dayRevenue,
                    dayRoas,
                    g.Sum(p => p.Clicks),
                    g.Sum(p => p.Impressions),
                    g.Sum(p => p.Conversions));
            })
            .ToList();

        // Quebra por Plataforma
        var platformBreakdown = currentFiltered
            .GroupBy(p => p.Platform)
            .Select(g =>
            {
                var platSpend = g.Sum(p => p.Spend);
                var platRevenue = g.Sum(p => p.Revenue);
                var platRoas = platSpend > 0 ? Math.Round(platRevenue / platSpend, 2) : 0m;
                var share = curSpend > 0 ? Math.Round((platSpend / curSpend) * 100m, 2) : 0m;
                return new PlatformShareResult(
                    g.Key,
                    platSpend,
                    platRevenue,
                    platRoas,
                    share,
                    g.Sum(p => p.Clicks),
                    g.Sum(p => p.Conversions));
            })
            .OrderByDescending(p => p.Spend)
            .ToList();

        // Quebra por Dispositivo
        var deviceBreakdown = currentFiltered
            .GroupBy(p => p.Device)
            .Select(g =>
            {
                var devSpend = g.Sum(p => p.Spend);
                var devClicks = g.Sum(p => p.Clicks);
                var devConversions = g.Sum(p => p.Conversions);
                var devRevenue = g.Sum(p => p.Revenue);
                var devRoas = devSpend > 0 ? Math.Round(devRevenue / devSpend, 2) : 0m;
                var share = curSpend > 0 ? Math.Round((devSpend / curSpend) * 100m, 2) : 0m;
                return new DeviceShareResult(
                    g.Key,
                    devSpend,
                    devClicks,
                    devConversions,
                    devRoas,
                    share);
            })
            .OrderByDescending(d => d.Spend)
            .ToList();

        var result = new ExecutiveDashboardResult(
            currentStartUtc,
            currentEndUtc,
            previousStartUtc,
            previousEndUtc,
            currency,
            metrics,
            timeSeries,
            platformBreakdown,
            deviceBreakdown);

        return Result<ExecutiveDashboardResult>.Success(result);
    }

    private static List<RawMetricPoint> FilterPoints(
        IEnumerable<RawMetricPoint> points,
        string? platformFilter,
        string? deviceFilter)
    {
        var list = points?.ToList() ?? new List<RawMetricPoint>();

        if (!string.IsNullOrWhiteSpace(platformFilter) && !platformFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            list = list.Where(p => p.Platform.Equals(platformFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(deviceFilter) && !deviceFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            list = list.Where(p => p.Device.Equals(deviceFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return list;
    }

    private static ExecutiveMetricItemResult BuildMetricItem(
        string metricKey,
        string label,
        decimal current,
        decimal previous,
        string unitFormat,
        bool isInvertedCost)
    {
        decimal deltaPercent = 0m;
        if (previous > 0)
        {
            deltaPercent = Math.Round(((current - previous) / previous) * 100m, 2);
        }
        else if (current > 0)
        {
            deltaPercent = 100m;
        }

        // Para custos (CPC, CPA, CPM): redução é melhoria (IsPositiveImprovement = true se delta <= 0)
        // Para métricas de performance (ROAS, CTR, Spend): aumento é melhoria
        bool isPositive = isInvertedCost ? deltaPercent <= 0 : deltaPercent >= 0;

        return new ExecutiveMetricItemResult(
            metricKey,
            label,
            current,
            previous,
            deltaPercent,
            isPositive,
            unitFormat);
    }
}
