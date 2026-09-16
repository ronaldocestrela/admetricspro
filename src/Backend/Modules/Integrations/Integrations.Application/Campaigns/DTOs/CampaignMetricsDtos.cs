namespace Integrations.Application.Campaigns.DTOs;

/// <summary>
/// Resumo executivo retornado após a conclusão da ingestão de métricas analíticas diárias e horárias.
/// </summary>
/// <param name="TotalAccountsProcessed">Quantidade de contas conectadas processadas.</param>
/// <param name="TotalRecordsIngested">Total de registros analíticos inseridos ou atualizados idempotentemente.</param>
/// <param name="TotalSpend">Investimento consolidado no lote.</param>
/// <param name="TotalImpressions">Total de impressões consolidadas.</param>
/// <param name="TotalClicks">Total de cliques consolidados.</param>
/// <param name="TotalConversions">Total de conversões consolidadas.</param>
/// <param name="TotalConversionValue">Receita financeira total consolidada.</param>
/// <param name="SyncedAtUtc">Carimbo UTC da conclusão do pipeline.</param>
/// <param name="Granularity">Granularidade temporal processada (Daily ou Hourly).</param>
/// <param name="SyncedPlatforms">Lista de plataformas sincronizadas com êxito.</param>
public sealed record SyncCampaignMetricsSummaryDto(
    int TotalAccountsProcessed,
    int TotalRecordsIngested,
    decimal TotalSpend,
    long TotalImpressions,
    long TotalClicks,
    decimal TotalConversions,
    decimal TotalConversionValue,
    DateTime SyncedAtUtc,
    string Granularity,
    IReadOnlyList<string> SyncedPlatforms);

/// <summary>
/// DTO de leitura de métricas analíticas de desempenho com KPIs calculados.
/// </summary>
/// <param name="Id">Identificador do registro.</param>
/// <param name="WorkspaceId">Identificador do workspace.</param>
/// <param name="ConnectedAdAccountId">Identificador da conta conectada.</param>
/// <param name="CampaignId">Identificador da campanha associada.</param>
/// <param name="AdSetId">Identificador opcional do conjunto.</param>
/// <param name="AdId">Identificador opcional do anúncio.</param>
/// <param name="Platform">Plataforma de anúncios.</param>
/// <param name="ExternalCampaignId">Identificador externo da campanha.</param>
/// <param name="ExternalAdSetId">Identificador externo do conjunto.</param>
/// <param name="ExternalAdId">Identificador externo do anúncio.</param>
/// <param name="Date">Data normalizada em UTC.</param>
/// <param name="Hour">Hora do dia (0 a 23) ou nulo.</param>
/// <param name="Granularity">Granularidade temporal (Daily ou Hourly).</param>
/// <param name="Spend">Investimento monetário.</param>
/// <param name="Currency">Moeda da conta.</param>
/// <param name="Impressions">Número de impressões.</param>
/// <param name="Clicks">Número de cliques.</param>
/// <param name="Conversions">Total de conversões.</param>
/// <param name="ConversionValue">Receita de conversão.</param>
/// <param name="Ctr">Taxa de cliques (%).</param>
/// <param name="Cpc">Custo por clique.</param>
/// <param name="Cpm">Custo por mil impressões.</param>
/// <param name="Cpa">Custo por aquisição.</param>
/// <param name="Roas">Retorno sobre investimento publicitário.</param>
/// <param name="SyncedAtUtc">Data/hora UTC da última sincronização.</param>
public sealed record CampaignMetricDto(
    Guid Id,
    Guid WorkspaceId,
    Guid ConnectedAdAccountId,
    Guid CampaignId,
    Guid? AdSetId,
    Guid? AdId,
    string Platform,
    string ExternalCampaignId,
    string? ExternalAdSetId,
    string? ExternalAdId,
    DateTime Date,
    int? Hour,
    string Granularity,
    decimal Spend,
    string Currency,
    long Impressions,
    long Clicks,
    decimal Conversions,
    decimal ConversionValue,
    decimal Ctr,
    decimal Cpc,
    decimal Cpm,
    decimal Cpa,
    decimal Roas,
    DateTime SyncedAtUtc);
