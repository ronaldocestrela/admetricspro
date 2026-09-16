namespace Analytics.Domain.Creatives;

/// <summary>
/// Métricas agregadas de desempenho de um criativo em uma rede de anúncios específica.
/// </summary>
/// <param name="Platform">Nome da plataforma de anúncios (ex: MetaAds, TikTokAds).</param>
/// <param name="AdCount">Quantidade de instâncias do anúncio veiculadas nesta plataforma.</param>
/// <param name="Spend">Investimento total alocado na plataforma.</param>
/// <param name="Impressions">Volume total de impressões geradas.</param>
/// <param name="Clicks">Volume total de cliques gerados.</param>
/// <param name="Conversions">Total de conversões auferidas.</param>
/// <param name="ConversionValue">Receita total auferida na plataforma.</param>
/// <param name="Ctr">Taxa de cliques agregada (CTR%).</param>
/// <param name="Cpc">Custo por clique médio.</param>
/// <param name="Cpa">Custo por conversão médio.</param>
/// <param name="Roas">Retorno sobre investimento publicitário médio.</param>
public sealed record CrossPlatformCreativeMetrics(
    string Platform,
    int AdCount,
    decimal Spend,
    long Impressions,
    long Clicks,
    decimal Conversions,
    decimal ConversionValue,
    decimal Ctr,
    decimal Cpc,
    decimal Cpa,
    decimal Roas);

/// <summary>
/// Comparativo consolidado de performance do mesmo ativo de mídia veiculado simultaneamente no Meta Ads vs. TikTok Ads.
/// </summary>
/// <param name="AssetFingerprint">Identificador canônico ou hash de mídia do criativo.</param>
/// <param name="AssetName">Nome de exibição do criativo compartilhado.</param>
/// <param name="PreviewUrl">URL de visualização / thumbnail da peça.</param>
/// <param name="MetaMetrics">Métricas de desempenho no Meta Ads (Instagram / Facebook).</param>
/// <param name="TikTokMetrics">Métricas de desempenho no TikTok Ads.</param>
/// <param name="WinningPlatform">Plataforma que entregou a melhor eficiência de conversão ou CPA.</param>
/// <param name="CpaDifferencePercentage">Diferença percentual de CPA entre as duas redes (valor positivo indica que o Meta foi mais barato, negativo indica TikTok mais barato).</param>
/// <param name="CtrDifferencePercentage">Diferença percentual de CTR entre as duas redes.</param>
/// <param name="EfficiencySummary">Resumo descritivo da recomendação estratégica de alocação de verba.</param>
public sealed record CrossPlatformComparisonResult(
    string AssetFingerprint,
    string AssetName,
    string? PreviewUrl,
    CrossPlatformCreativeMetrics MetaMetrics,
    CrossPlatformCreativeMetrics TikTokMetrics,
    string WinningPlatform,
    decimal CpaDifferencePercentage,
    decimal CtrDifferencePercentage,
    string EfficiencySummary);
