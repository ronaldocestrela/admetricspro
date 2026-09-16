namespace Analytics.Domain.Blended;

/// <summary>
/// Item de entrada que representa uma métrica consolidada de campanha, conjunto ou anúncio
/// a ser processada pelo motor de métricas blended e MER.
/// </summary>
/// <param name="Platform">Plataforma de anúncios de origem (ex: Meta, Google, TikTok, Bing).</param>
/// <param name="CampaignId">Identificador único opcional da campanha no sistema.</param>
/// <param name="ExternalCampaignId">Identificador externo da campanha na rede de anúncios.</param>
/// <param name="Date">Data de referência da métrica de veiculação.</param>
/// <param name="Spend">Investimento financeiro realizado no período na moeda de origem.</param>
/// <param name="Currency">Moeda em que o investimento e as receitas foram faturados (ex: USD, BRL, EUR).</param>
/// <param name="Impressions">Número total de impressões veiculadas.</param>
/// <param name="Clicks">Número total de cliques contabilizados.</param>
/// <param name="Conversions">Volume total de conversões atribuídas.</param>
/// <param name="ConversionValue">Receita monetária bruta gerada pelas conversões na moeda de origem.</param>
/// <param name="NewCustomers">Número opcional de novos clientes adquiridos diretamente.</param>
public sealed record BlendedMetricInputItem(
    string Platform,
    Guid? CampaignId,
    string? ExternalCampaignId,
    DateTime Date,
    decimal Spend,
    string Currency,
    long Impressions,
    long Clicks,
    decimal Conversions,
    decimal ConversionValue,
    int? NewCustomers = null);

/// <summary>
/// Representa o detalhamento analítico e percentual de participação de uma rede de anúncios
/// dentro do investimento consolidado total.
/// </summary>
/// <param name="Platform">Nome da plataforma de anúncios.</param>
/// <param name="Spend">Total investido na plataforma normalizado na moeda alvo.</param>
/// <param name="SpendSharePercentage">Percentual do gasto desta plataforma em relação ao gasto total consolidado (0 a 100%).</param>
/// <param name="Impressions">Total de impressões na plataforma.</param>
/// <param name="Clicks">Total de cliques na plataforma.</param>
/// <param name="Conversions">Total de conversões registradas na plataforma.</param>
/// <param name="ConversionValue">Receita gerada pelas conversões na plataforma normalizada na moeda alvo.</param>
/// <param name="Roas">ROAS individual da plataforma (ConversionValue / Spend).</param>
/// <param name="Cpa">Custo por aquisição/conversão individual da plataforma (Spend / Conversions).</param>
/// <param name="Cpc">Custo por clique individual (Spend / Clicks).</param>
/// <param name="Cpm">Custo por mil impressões individual ((Spend / Impressions) * 1000).</param>
/// <param name="Ctr">Taxa de cliques individual ((Clicks / Impressions) * 100).</param>
public sealed record BlendedChannelBreakdown(
    string Platform,
    decimal Spend,
    decimal SpendSharePercentage,
    long Impressions,
    long Clicks,
    decimal Conversions,
    decimal ConversionValue,
    decimal Roas,
    decimal Cpa,
    decimal Cpc,
    decimal Cpm,
    decimal Ctr);

/// <summary>
/// Resultado consolidado do cálculo de métricas agregadas multi-canal (MER, Blended ROAS, Blended CAC).
/// </summary>
/// <param name="TargetCurrency">Moeda unificada do resultado analítico (ex: BRL, USD).</param>
/// <param name="TotalSpend">Investimento consolidado de todas as plataformas na moeda alvo.</param>
/// <param name="TotalConversionValue">Receita consolidada de conversões de todas as plataformas na moeda alvo.</param>
/// <param name="TotalStoreRevenue">Receita global da loja/e-commerce (se informada para cálculo estrito de MER).</param>
/// <param name="TotalImpressions">Volume agregado de impressões de todas as redes.</param>
/// <param name="TotalClicks">Volume agregado de cliques de todas as redes.</param>
/// <param name="TotalConversions">Volume agregado de conversões registradas em todas as redes.</param>
/// <param name="TotalNewCustomers">Total de novos clientes adquiridos no período consolidado.</param>
/// <param name="MarketingEfficiencyRatio">Marketing Efficiency Ratio (MER): Receita Total (Loja ou Conversões) / Gasto Total Consolidado.</param>
/// <param name="BlendedRoas">Blended ROAS: Receita Total de Conversões / Gasto Total Consolidado.</param>
/// <param name="BlendedCac">Blended CAC: Gasto Total Consolidado / Total de Novos Clientes.</param>
/// <param name="BlendedCpa">Blended CPA: Gasto Total Consolidado / Total de Conversões.</param>
/// <param name="BlendedCpc">Blended CPC: Gasto Total Consolidado / Total de Cliques.</param>
/// <param name="BlendedCpm">Blended CPM: (Gasto Total Consolidado / Total de Impressões) * 1000.</param>
/// <param name="BlendedCtr">Blended CTR: (Total de Cliques / Total de Impressões) * 100.</param>
/// <param name="ChannelBreakdowns">Detalhamento individual de cada plataforma participante com percentuais de share.</param>
public sealed record BlendedMetricsResult(
    string TargetCurrency,
    decimal TotalSpend,
    decimal TotalConversionValue,
    decimal? TotalStoreRevenue,
    long TotalImpressions,
    long TotalClicks,
    decimal TotalConversions,
    int TotalNewCustomers,
    decimal MarketingEfficiencyRatio,
    decimal BlendedRoas,
    decimal BlendedCac,
    decimal BlendedCpa,
    decimal BlendedCpc,
    decimal BlendedCpm,
    decimal BlendedCtr,
    IReadOnlyList<BlendedChannelBreakdown> ChannelBreakdowns);
