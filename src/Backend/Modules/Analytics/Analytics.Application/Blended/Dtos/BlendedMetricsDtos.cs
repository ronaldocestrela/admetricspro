namespace Analytics.Application.Blended.Dtos;

/// <summary>
/// Item de entrada para a consulta de consolidação de métricas blended e MER.
/// </summary>
/// <param name="Platform">Plataforma de anúncios (Meta, Google, TikTok, Bing, etc.).</param>
/// <param name="CampaignId">Identificador único da campanha no sistema (opcional).</param>
/// <param name="ExternalCampaignId">Identificador externo na rede de anúncios (opcional).</param>
/// <param name="Date">Data da veiculação.</param>
/// <param name="Spend">Investimento financeiro realizado.</param>
/// <param name="Currency">Código ISO da moeda do gasto e receitas (ex: BRL, USD, EUR).</param>
/// <param name="Impressions">Volume de impressões.</param>
/// <param name="Clicks">Volume de cliques.</param>
/// <param name="Conversions">Volume de conversões.</param>
/// <param name="ConversionValue">Receita monetária gerada pelas conversões.</param>
/// <param name="NewCustomers">Quantidade de novos clientes adquiridos diretamente (opcional).</param>
public sealed record BlendedMetricItemInput(
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
/// Detalhamento analítico individual por plataforma de anúncios com percentual de participação do investimento.
/// </summary>
/// <param name="Platform">Nome da plataforma de anúncios.</param>
/// <param name="Spend">Total investido na moeda unificada alvo.</param>
/// <param name="SpendSharePercentage">Percentual do gasto em relação ao investimento total consolidado.</param>
/// <param name="Impressions">Volume de impressões veiculadas.</param>
/// <param name="Clicks">Volume de cliques gerados.</param>
/// <param name="Conversions">Volume de conversões registradas.</param>
/// <param name="ConversionValue">Receita gerada pelas conversões na moeda unificada alvo.</param>
/// <param name="Roas">ROAS individual da plataforma.</param>
/// <param name="Cpa">Custo por aquisição/conversão individual.</param>
/// <param name="Cpc">Custo por clique individual.</param>
/// <param name="Cpm">Custo por mil impressões individual.</param>
/// <param name="Ctr">Taxa de cliques individual em porcentagem.</param>
public sealed record BlendedChannelBreakdownDto(
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
/// DTO com resultado financeiro e analítico consolidado multi-canal (MER, Blended ROAS, Blended CAC).
/// </summary>
/// <param name="TargetCurrency">Moeda unificada do resultado analítico (ex: BRL, USD).</param>
/// <param name="TotalSpend">Gasto total consolidado em mídia em todas as redes na moeda alvo.</param>
/// <param name="TotalConversionValue">Receita total de conversões geradas nas redes de anúncio na moeda alvo.</param>
/// <param name="TotalStoreRevenue">Receita bruta global do e-commerce/loja externa (se informada).</param>
/// <param name="TotalImpressions">Total de impressões veiculadas somadas.</param>
/// <param name="TotalClicks">Total de cliques contabilizados somados.</param>
/// <param name="TotalConversions">Total de conversões registradas somadas.</param>
/// <param name="TotalNewCustomers">Total de novos clientes adquiridos no período.</param>
/// <param name="MarketingEfficiencyRatio">Marketing Efficiency Ratio (MER): Receita Total / Gasto Total Consolidado.</param>
/// <param name="BlendedRoas">Blended ROAS: Receita de Conversões / Gasto Total Consolidado.</param>
/// <param name="BlendedCac">Blended CAC: Gasto Total Consolidado / Total de Novos Clientes.</param>
/// <param name="BlendedCpa">Blended CPA: Gasto Total Consolidado / Total de Conversões.</param>
/// <param name="BlendedCpc">Blended CPC: Gasto Total Consolidado / Total de Cliques.</param>
/// <param name="BlendedCpm">Blended CPM: (Gasto Total Consolidado / Total de Impressões) * 1000.</param>
/// <param name="BlendedCtr">Blended CTR: (Total de Cliques / Total de Impressões) * 100.</param>
/// <param name="ChannelBreakdowns">Lista com o detalhamento analítico e participação de cada plataforma.</param>
public sealed record BlendedMetricsDto(
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
    IReadOnlyList<BlendedChannelBreakdownDto> ChannelBreakdowns);
