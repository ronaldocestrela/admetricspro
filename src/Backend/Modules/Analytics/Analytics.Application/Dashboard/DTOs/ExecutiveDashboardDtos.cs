using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Dashboard.DTOs;

/// <summary>
/// Representa um cartão individual de métrica executiva com comparação temporal em relação ao período anterior.
/// </summary>
/// <param name="MetricKey">Chave identificadora única da métrica (ex.: Spend, Cpc, Cpm, Ctr, Cpa, Roas).</param>
/// <param name="Label">Rótulo textual legível para apresentação em tela.</param>
/// <param name="CurrentValue">Valor consolidado no período ativo filtrado.</param>
/// <param name="PreviousValue">Valor consolidado no período comparativo anterior.</param>
/// <param name="PercentageChange">Variação percentual relativa (ex.: +12.5% ou -5.2%).</param>
/// <param name="IsPositiveImprovement">Indica se a variação representa melhoria de negócio (respeitando polaridade invertida para custos como CPA e CPC).</param>
/// <param name="UnitFormat">Formato de unidade (Currency, Percentage, Decimal, Multiplier).</param>
public sealed record ExecutiveMetricItemDto(
    string MetricKey,
    string Label,
    decimal CurrentValue,
    decimal PreviousValue,
    decimal PercentageChange,
    bool IsPositiveImprovement,
    string UnitFormat);

/// <summary>
/// Ponto temporal na série histórica diária para gráficos interativos de evolução de desempenho.
/// </summary>
/// <param name="Date">Data do registro em UTC.</param>
/// <param name="Spend">Investimento consolidado no dia.</param>
/// <param name="Revenue">Receita ou valor de conversão gerado no dia.</param>
/// <param name="Roas">Retorno sobre investimento publicitário no dia.</param>
/// <param name="Clicks">Total de cliques contabilizados.</param>
/// <param name="Impressions">Total de impressões exibidas.</param>
/// <param name="Conversions">Total de conversões registradas.</param>
public sealed record ExecutiveTimeSeriesPointDto(
    DateTime Date,
    decimal Spend,
    decimal Revenue,
    decimal Roas,
    long Clicks,
    long Impressions,
    decimal Conversions);

/// <summary>
/// Distribuição e participação de investimento e performance por canal de anúncios (Meta, Google, TikTok, Bing).
/// </summary>
/// <param name="Platform">Nome da plataforma de anúncios.</param>
/// <param name="Spend">Investimento alocado na plataforma.</param>
/// <param name="Revenue">Receita atribuída à plataforma.</param>
/// <param name="Roas">ROAS obtido no canal.</param>
/// <param name="ShareOfSpendPercentage">Participação percentual do canal sobre o orçamento total investido.</param>
/// <param name="Clicks">Total de cliques gerados pela plataforma.</param>
/// <param name="Conversions">Total de conversões geradas pela plataforma.</param>
public sealed record PlatformShareDto(
    string Platform,
    decimal Spend,
    decimal Revenue,
    decimal Roas,
    decimal ShareOfSpendPercentage,
    long Clicks,
    decimal Conversions);

/// <summary>
/// Distribuição de desempenho consolidado por categoria de dispositivo de acesso (Mobile, Desktop, Tablet).
/// </summary>
/// <param name="Device">Categoria do dispositivo (Mobile, Desktop, Tablet).</param>
/// <param name="Spend">Investimento associado ao dispositivo.</param>
/// <param name="Clicks">Total de cliques originados do dispositivo.</param>
/// <param name="Conversions">Total de conversões originadas do dispositivo.</param>
/// <param name="Roas">ROAS médio por dispositivo.</param>
/// <param name="ShareOfSpendPercentage">Participação percentual do dispositivo no investimento total.</param>
public sealed record DeviceShareDto(
    string Device,
    decimal Spend,
    long Clicks,
    decimal Conversions,
    decimal Roas,
    decimal ShareOfSpendPercentage);

/// <summary>
/// Resumo executivo consolidado para o dashboard unificado cross-network, incluindo cartões de KPIs com comparativo,
/// série temporal diária e quebras por plataforma e dispositivo.
/// </summary>
/// <param name="StartDateUtc">Início do período corrente analisado.</param>
/// <param name="EndDateUtc">Fim do período corrente analisado.</param>
/// <param name="PreviousStartDateUtc">Início do período anterior equivalente para comparação.</param>
/// <param name="PreviousEndDateUtc">Fim do período anterior equivalente para comparação.</param>
/// <param name="Currency">Moeda monetária padrão da visualização (ex.: BRL, USD).</param>
/// <param name="Metrics">Lista de cartões de métricas principais (Spend, CPC, CPM, CTR, CPA, ROAS).</param>
/// <param name="TimeSeries">Série temporal diária para plotagem de tendências e gráficos.</param>
/// <param name="PlatformBreakdown">Distribuição proporcional por plataforma de anúncios.</param>
/// <param name="DeviceBreakdown">Distribuição proporcional por tipo de dispositivo.</param>
public sealed record ExecutiveDashboardDto(
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    DateTime PreviousStartDateUtc,
    DateTime PreviousEndDateUtc,
    string Currency,
    IReadOnlyList<ExecutiveMetricItemDto> Metrics,
    IReadOnlyList<ExecutiveTimeSeriesPointDto> TimeSeries,
    IReadOnlyList<PlatformShareDto> PlatformBreakdown,
    IReadOnlyList<DeviceShareDto> DeviceBreakdown);
