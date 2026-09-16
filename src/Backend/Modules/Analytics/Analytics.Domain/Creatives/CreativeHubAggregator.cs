using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Creatives;

/// <summary>
/// Agregador analítico de ativos de mídia cross-platform.
/// Consolida métricas por peça publicitária veiculada simultaneamente em redes distintas (Meta Ads vs. TikTok Ads).
/// </summary>
public sealed class CreativeHubAggregator : ICreativeHubAggregator
{
    /// <inheritdoc />
    public Result<CrossPlatformComparisonResult> CompareCrossPlatform(
        string assetFingerprint,
        string assetName,
        string? previewUrl,
        IReadOnlyList<CreativeDailyMetricPoint> metaMetrics,
        IReadOnlyList<CreativeDailyMetricPoint> tikTokMetrics)
    {
        if (string.IsNullOrWhiteSpace(assetFingerprint))
        {
            return Result<CrossPlatformComparisonResult>.Failure(
                Error.Validation("CreativeComparison.InvalidFingerprint", "O identificador do ativo de mídia é obrigatório."));
        }

        var hasMeta = metaMetrics != null && metaMetrics.Count > 0;
        var hasTikTok = tikTokMetrics != null && tikTokMetrics.Count > 0;

        if (!hasMeta && !hasTikTok)
        {
            return Result<CrossPlatformComparisonResult>.Failure(
                Error.Validation("CreativeComparison.NoData", "Nenhuma métrica foi localizada para o ativo de mídia informado."));
        }

        var metaConsolidated = AggregateMetrics("MetaAds", metaMetrics ?? Array.Empty<CreativeDailyMetricPoint>());
        var tikTokConsolidated = AggregateMetrics("TikTokAds", tikTokMetrics ?? Array.Empty<CreativeDailyMetricPoint>());

        string winningPlatform;
        decimal cpaDifferencePercentage = 0m;
        decimal ctrDifferencePercentage = 0m;
        string efficiencySummary;

        if (metaConsolidated.Conversions > 0 && tikTokConsolidated.Conversions > 0)
        {
            cpaDifferencePercentage = Math.Round(((metaConsolidated.Cpa - tikTokConsolidated.Cpa) / tikTokConsolidated.Cpa) * 100m, 2);

            if (metaConsolidated.Cpa < tikTokConsolidated.Cpa)
            {
                winningPlatform = "MetaAds";
                efficiencySummary = $"{winningPlatform} entregou melhor custo por conversão (CPA de R$ {metaConsolidated.Cpa:F2} vs. R$ {tikTokConsolidated.Cpa:F2} no TikTokAds). Sugerida concentração de orçamento no {winningPlatform} para este criativo.";
            }
            else if (tikTokConsolidated.Cpa < metaConsolidated.Cpa)
            {
                winningPlatform = "TikTokAds";
                efficiencySummary = $"{winningPlatform} superou o MetaAds em eficiência de aquisição (CPA de R$ {tikTokConsolidated.Cpa:F2} vs. R$ {metaConsolidated.Cpa:F2} no MetaAds, com ROAS de {tikTokConsolidated.Roas:F2}x). Sugerido redirecionamento de verba para {winningPlatform}.";
            }
            else
            {
                winningPlatform = metaConsolidated.Roas >= tikTokConsolidated.Roas ? "MetaAds" : "TikTokAds";
                efficiencySummary = $"Ambas as redes apresentaram paridade de CPA. Desempate aplicado pelo ROAS ({winningPlatform}).";
            }
        }
        else if (metaConsolidated.Conversions > 0)
        {
            winningPlatform = "MetaAds";
            efficiencySummary = "Apenas Meta Ads converteu clientes com este criativo no período. Mantenha os testes no TikTok apenas se o objetivo for topo de funil.";
        }
        else if (tikTokConsolidated.Conversions > 0)
        {
            winningPlatform = "TikTokAds";
            efficiencySummary = "Apenas TikTok Ads gerou conversões comprovadas com esta peça criativa. Recomenda-se escalar o investimento no TikTok.";
        }
        else
        {
            if (metaConsolidated.Ctr >= tikTokConsolidated.Ctr)
            {
                winningPlatform = "MetaAds";
                efficiencySummary = $"Sem conversões registradas em ambas as redes. Meta Ads obteve melhor CTR de engajamento ({metaConsolidated.Ctr:F2}% vs. {tikTokConsolidated.Ctr:F2}%).";
            }
            else
            {
                winningPlatform = "TikTokAds";
                efficiencySummary = $"Sem conversões registradas em ambas as redes. TikTok Ads obteve maior taxa de cliques de engajamento ({tikTokConsolidated.Ctr:F2}% vs. {metaConsolidated.Ctr:F2}%).";
            }
        }

        if (tikTokConsolidated.Ctr > 0)
        {
            ctrDifferencePercentage = Math.Round(((metaConsolidated.Ctr - tikTokConsolidated.Ctr) / tikTokConsolidated.Ctr) * 100m, 2);
        }

        var result = new CrossPlatformComparisonResult(
            assetFingerprint,
            assetName,
            previewUrl,
            metaConsolidated,
            tikTokConsolidated,
            winningPlatform,
            cpaDifferencePercentage,
            ctrDifferencePercentage,
            efficiencySummary);

        return Result<CrossPlatformComparisonResult>.Success(result);
    }

    private static CrossPlatformCreativeMetrics AggregateMetrics(string platform, IReadOnlyList<CreativeDailyMetricPoint> points)
    {
        if (points.Count == 0)
        {
            return new CrossPlatformCreativeMetrics(platform, 0, 0m, 0, 0, 0m, 0m, 0m, 0m, 0m, 0m);
        }

        var spend = points.Sum(p => p.Spend);
        var impressions = points.Sum(p => p.Impressions);
        var clicks = points.Sum(p => p.Clicks);
        var conversions = points.Sum(p => p.Conversions);
        var conversionValue = points.Sum(p => p.ConversionValue);

        var ctr = impressions > 0
            ? Math.Round(((decimal)clicks / impressions) * 100m, 4)
            : 0m;

        var cpc = clicks > 0
            ? Math.Round(spend / clicks, 4)
            : 0m;

        var cpa = conversions > 0
            ? Math.Round(spend / conversions, 4)
            : 0m;

        var roas = spend > 0
            ? Math.Round(conversionValue / spend, 4)
            : 0m;

        return new CrossPlatformCreativeMetrics(
            platform,
            AdCount: 1,
            spend,
            impressions,
            clicks,
            conversions,
            conversionValue,
            ctr,
            cpc,
            cpa,
            roas);
    }
}
