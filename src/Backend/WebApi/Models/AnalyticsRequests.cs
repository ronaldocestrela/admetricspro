namespace WebApi.Models;

/// <summary>
/// Parâmetros de requisição para conversão monetária individual.
/// </summary>
/// <param name="Amount">Valor financeiro a converter.</param>
/// <param name="SourceCurrency">Código ISO da moeda de origem (ex: USD, EUR, BRL).</param>
/// <param name="TargetCurrency">Código ISO da moeda de destino (ex: BRL, USD).</param>
/// <param name="Date">Data de referência histórica ou corrente para a cotação.</param>
public sealed record ConvertCurrencyApiRequest(
    decimal Amount,
    string SourceCurrency,
    string TargetCurrency,
    DateTime Date);

/// <summary>
/// Item individual de valor a converter em lote.
/// </summary>
/// <param name="Amount">Valor financeiro.</param>
/// <param name="SourceCurrency">Moeda de origem.</param>
/// <param name="Date">Data da cotação.</param>
public sealed record CurrencyBatchItemApiRequest(
    decimal Amount,
    string SourceCurrency,
    DateTime Date);

/// <summary>
/// Parâmetros de requisição para conversão monetária em lote.
/// </summary>
/// <param name="Items">Lista de itens a serem convertidos.</param>
/// <param name="TargetCurrency">Moeda de destino unificada.</param>
public sealed record ConvertCurrencyBatchApiRequest(
    IReadOnlyList<CurrencyBatchItemApiRequest> Items,
    string TargetCurrency);

/// <summary>
/// Parâmetros de requisição para classificação taxonômica individual.
/// </summary>
/// <param name="CampaignName">Nome da campanha de tráfego pago.</param>
/// <param name="AdSetName">Nome opcional do conjunto de anúncios.</param>
/// <param name="AdName">Nome opcional do anúncio / criativo.</param>
/// <param name="ReferenceId">Identificador opcional de correlação.</param>
public sealed record ClassifyTaxonomyApiRequest(
    string CampaignName,
    string? AdSetName = null,
    string? AdName = null,
    string? ReferenceId = null);

/// <summary>
/// Parâmetros de requisição para classificação taxonômica em lote.
/// </summary>
/// <param name="Items">Lista de campanhas ou anúncios a classificar.</param>
public sealed record BatchClassifyTaxonomyApiRequest(
    IReadOnlyList<ClassifyTaxonomyApiRequest> Items);

/// <summary>
/// Item de métrica de campanha individual para consolidação em métricas blended e MER.
/// </summary>
/// <param name="Platform">Plataforma de anúncios (Meta, Google, TikTok, Bing, etc.).</param>
/// <param name="CampaignId">Identificador único opcional da campanha no sistema.</param>
/// <param name="ExternalCampaignId">Identificador externo da campanha na rede de anúncios.</param>
/// <param name="Date">Data de referência da veiculação.</param>
/// <param name="Spend">Investimento financeiro realizado.</param>
/// <param name="Currency">Código ISO da moeda do valor de investimento e retorno (ex: BRL, USD).</param>
/// <param name="Impressions">Número total de impressões veiculadas.</param>
/// <param name="Clicks">Número total de cliques contabilizados.</param>
/// <param name="Conversions">Volume total de conversões atribuídas.</param>
/// <param name="ConversionValue">Receita monetária gerada pelas conversões.</param>
/// <param name="NewCustomers">Total opcional de novos clientes adquiridos.</param>
public sealed record BlendedMetricItemApiRequest(
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
/// Parâmetros de requisição para consolidação de métricas agregadas multi-canal (MER, Blended ROAS, Blended CAC).
/// </summary>
/// <param name="Items">Lista de métricas das plataformas a serem consolidadas.</param>
/// <param name="TargetCurrency">Código ISO da moeda destino de consolidação (padrão: BRL).</param>
/// <param name="TotalStoreRevenue">Receita bruta global do e-commerce/loja externa para cálculo de MER estrito (opcional).</param>
/// <param name="TotalNewCustomers">Total global de novos clientes adquiridos no período consolidado (opcional).</param>
public sealed record CalculateBlendedMetricsApiRequest(
    IReadOnlyList<BlendedMetricItemApiRequest> Items,
    string TargetCurrency = "BRL",
    decimal? TotalStoreRevenue = null,
    int? TotalNewCustomers = null);

/// <summary>
/// Ponto de contato de mídia na jornada do usuário para cálculo de atribuição.
/// </summary>
/// <param name="Channel">Nome da plataforma ou canal de marketing (ex: Meta, Google, TikTok, Bing).</param>
/// <param name="CampaignName">Nome descritivo da campanha (opcional).</param>
/// <param name="OccurredAtUtc">Data e hora do toque em UTC.</param>
/// <param name="TouchType">Tipo da interação (1 para Click, 2 para Impression).</param>
/// <param name="Cost">Custo atribuído à interação individual (se disponível).</param>
public sealed record AttributionTouchpointApiRequest(
    string Channel,
    string? CampaignName,
    DateTime OccurredAtUtc,
    int TouchType = 1,
    decimal Cost = 0m);

/// <summary>
/// Jornada de conversão percorrida por um cliente ou transação.
/// </summary>
/// <param name="JourneyId">Identificador único da jornada ou pedido.</param>
/// <param name="CustomerId">Identificador único ou pseudônimo do cliente/lead (opcional).</param>
/// <param name="ConvertedAtUtc">Data e hora exata em UTC da conversão.</param>
/// <param name="ConversionValue">Valor financeiro gerado pela conversão.</param>
/// <param name="Touchpoints">Lista de pontos de contato prévios à conversão.</param>
public sealed record ConversionJourneyApiRequest(
    string JourneyId,
    string? CustomerId,
    DateTime ConvertedAtUtc,
    decimal ConversionValue,
    IReadOnlyList<AttributionTouchpointApiRequest> Touchpoints);

/// <summary>
/// Parâmetros de requisição para análise e comparação de modelos de atribuição multi-canal.
/// </summary>
/// <param name="Journeys">Lista de jornadas de conversão com seus respectivos pontos de contato.</param>
/// <param name="ModelType">Modelo opcional específico (1 = FirstTouch, 2 = LastTouch, 3 = Linear; nulo = compara os três).</param>
/// <param name="ChannelCosts">Dicionário opcional de investimentos totais por canal para cálculo de ROAS e CPA atribuídos.</param>
public sealed record CalculateAttributionApiRequest(
    IReadOnlyList<ConversionJourneyApiRequest> Journeys,
    int? ModelType = null,
    IReadOnlyDictionary<string, decimal>? ChannelCosts = null);
