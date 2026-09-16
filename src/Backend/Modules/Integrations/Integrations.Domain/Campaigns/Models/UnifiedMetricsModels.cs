namespace Integrations.Domain.Campaigns.Models;

/// <summary>
/// Representa uma métrica analítica unificada extraída de uma rede de anúncios externa.
/// </summary>
/// <param name="ExternalCampaignId">Identificador da campanha na plataforma de anúncios.</param>
/// <param name="ExternalAdSetId">Identificador opcional do conjunto de anúncios na plataforma.</param>
/// <param name="ExternalAdId">Identificador opcional do anúncio/criativo na plataforma.</param>
/// <param name="Date">Data normalizada em UTC (meia-noite).</param>
/// <param name="Hour">Hora do dia (0 a 23) para métricas horárias, nulo para diárias.</param>
/// <param name="Spend">Investimento monetário.</param>
/// <param name="Impressions">Número de impressões.</param>
/// <param name="Clicks">Número de cliques.</param>
/// <param name="Conversions">Número de conversões.</param>
/// <param name="ConversionValue">Valor financeiro total retornado pelas conversões.</param>
/// <param name="Currency">Código ISO da moeda (ex: BRL, USD).</param>
public sealed record UnifiedMetricItem(
    string ExternalCampaignId,
    string? ExternalAdSetId,
    string? ExternalAdId,
    DateTime Date,
    int? Hour,
    decimal Spend,
    long Impressions,
    long Clicks,
    decimal Conversions,
    decimal ConversionValue,
    string Currency);
