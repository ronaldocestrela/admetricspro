using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Creatives;

/// <summary>
/// Motor analítico de cálculo e detecção de fadiga e saturação de criativos publicitários.
/// Identifica anúncios cujo CTR caiu progressivamente na janela temporal associado a frequência elevada.
/// </summary>
public sealed class AdFatigueDetector : IAdFatigueDetector
{
    private const int MinimumDaysRequired = 3;
    private const decimal CriticalFatigueCtrDropPercentage = -20.0m;
    private const decimal WarningFatigueCtrDropPercentage = -10.0m;
    private const decimal CriticalFrequencyThreshold = 2.0m;

    /// <inheritdoc />
    public Result<CreativeFatigueAnalysisResult> AnalyzeFatigue(
        Guid adId,
        string adName,
        string platform,
        string? previewUrl,
        IReadOnlyList<CreativeDailyMetricPoint> dailyMetrics)
    {
        if (adId == Guid.Empty)
        {
            return Result<CreativeFatigueAnalysisResult>.Failure(
                Error.Validation("CreativeFatigue.InvalidAdId", "O identificador do anúncio é obrigatório."));
        }

        if (dailyMetrics is null || dailyMetrics.Count < MinimumDaysRequired)
        {
            return Result<CreativeFatigueAnalysisResult>.Failure(
                Error.Validation("CreativeFatigue.InsufficientData", $"São necessários ao menos {MinimumDaysRequired} dias de métricas para calcular a tendência de fadiga."));
        }

        var totalImpressions = dailyMetrics.Sum(m => m.Impressions);
        if (totalImpressions <= 0)
        {
            return Result<CreativeFatigueAnalysisResult>.Failure(
                Error.Validation("CreativeFatigue.NoImpressions", "O anúncio não possui impressões registradas no período analisado."));
        }

        var orderedMetrics = dailyMetrics.OrderBy(m => m.Date).ToList();
        var analyzedDays = orderedMetrics.Count;

        var initialCtr = orderedMetrics[0].Ctr;
        var currentCtr = orderedMetrics[^1].Ctr;

        var ctrChangePercentage = initialCtr > 0
            ? Math.Round(((currentCtr - initialCtr) / initialCtr) * 100m, 2)
            : 0m;

        var ctrTrendSlope = CalculateLinearRegressionSlope(orderedMetrics.Select(m => (double)m.Ctr).ToList());

        var validFrequencies = orderedMetrics.Where(m => m.Frequency > 0).Select(m => m.Frequency).ToList();
        var averageFrequency = validFrequencies.Count > 0
            ? Math.Round(validFrequencies.Average(), 2)
            : 1.0m;

        var currentFrequency = orderedMetrics[^1].Frequency > 0
            ? orderedMetrics[^1].Frequency
            : averageFrequency;

        CreativeFatigueStatus status;
        bool replacementSuggested;
        string reason;
        string actionRecommendation;

        var hasCriticalCtrDrop = ctrChangePercentage <= CriticalFatigueCtrDropPercentage;
        var hasDownwardSlope = ctrTrendSlope < -0.01;
        var isFrequencyHigh = averageFrequency >= CriticalFrequencyThreshold || currentFrequency >= CriticalFrequencyThreshold;

        if (hasCriticalCtrDrop && isFrequencyHigh && hasDownwardSlope)
        {
            status = CreativeFatigueStatus.Fatigued;
            replacementSuggested = true;
            reason = $"Fadiga crítica detectada: queda de {Math.Abs(ctrChangePercentage):F1}% no CTR nos últimos {analyzedDays} dias acompanhada de frequência média elevada ({averageFrequency:F1}x). O público atingido está saturado.";
            actionRecommendation = "Substitua a peça publicitária por uma nova variação ou renove os ganchos visuais e textos imediatamente para estancar a degradação de CPA.";
        }
        else if (ctrChangePercentage <= WarningFatigueCtrDropPercentage || isFrequencyHigh || hasDownwardSlope)
        {
            status = CreativeFatigueStatus.Warning;
            replacementSuggested = false;
            reason = $"Sinais de desgaste preliminar: variação de {ctrChangePercentage:F1}% no CTR e frequência média em {averageFrequency:F1}x ao longo dos últimos {analyzedDays} dias.";
            actionRecommendation = "Monitore as métricas do criativo nos próximos dias e prepare variações na esteira de produção para substituição preventiva.";
        }
        else
        {
            status = CreativeFatigueStatus.Healthy;
            replacementSuggested = false;
            reason = $"Criativo com performance saudável: CTR estabilizado em {currentCtr:F2}% e frequência sob controle em {averageFrequency:F1}x.";
            actionRecommendation = "Mantenha a veiculação regular do criativo. Avalie escala de orçamento caso os indicadores de conversão permaneçam positivos.";
        }

        var result = new CreativeFatigueAnalysisResult(
            adId,
            adName,
            platform,
            previewUrl,
            status,
            analyzedDays,
            initialCtr,
            currentCtr,
            ctrChangePercentage,
            (decimal)ctrTrendSlope,
            averageFrequency,
            currentFrequency,
            replacementSuggested,
            reason,
            actionRecommendation);

        return Result<CreativeFatigueAnalysisResult>.Success(result);
    }

    private static double CalculateLinearRegressionSlope(IReadOnlyList<double> values)
    {
        var n = values.Count;
        if (n < 2) return 0;

        double sumX = 0;
        double sumY = 0;
        double sumXy = 0;
        double sumXSquare = 0;

        for (int i = 0; i < n; i++)
        {
            double x = i;
            double y = values[i];

            sumX += x;
            sumY += y;
            sumXy += x * y;
            sumXSquare += x * x;
        }

        double denominator = (n * sumXSquare) - (sumX * sumX);
        if (Math.Abs(denominator) < 0.0000001) return 0;

        return Math.Round(((n * sumXy) - (sumX * sumY)) / denominator, 4);
    }
}
