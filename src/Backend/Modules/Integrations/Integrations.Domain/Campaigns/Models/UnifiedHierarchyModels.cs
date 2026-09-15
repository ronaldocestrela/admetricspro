using BuildingBlocks.Domain.Campaigns;

namespace Integrations.Domain.Campaigns.Models;

/// <summary>
/// Representa uma campanha normalizada vinda de uma rede de anúncios externa.
/// </summary>
/// <param name="ExternalCampaignId">Identificador na plataforma de origem.</param>
/// <param name="Name">Nome da campanha.</param>
/// <param name="Status">Status unificado.</param>
/// <param name="Objective">Objetivo de marketing.</param>
/// <param name="DailyBudget">Orçamento diário normalizado em moeda.</param>
/// <param name="LifetimeBudget">Orçamento vitalício normalizado em moeda.</param>
/// <param name="Currency">Código ISO da moeda (ex: BRL, USD).</param>
/// <param name="StartDateUtc">Data de início da veiculação.</param>
/// <param name="EndDateUtc">Data de término da veiculação.</param>
public sealed record UnifiedCampaignItem(
    string ExternalCampaignId,
    string Name,
    CampaignStatus Status,
    string Objective,
    decimal? DailyBudget,
    decimal? LifetimeBudget,
    string Currency,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc);

/// <summary>
/// Representa um conjunto ou grupo de anúncios normalizado (AdSet / AdGroup).
/// </summary>
/// <param name="ExternalAdSetId">Identificador do conjunto na plataforma de origem.</param>
/// <param name="ExternalCampaignId">Identificador da campanha pai.</param>
/// <param name="Name">Nome do conjunto.</param>
/// <param name="Status">Status unificado.</param>
/// <param name="BidStrategy">Estratégia de lances.</param>
/// <param name="OptimizationGoal">Objetivo de otimização.</param>
/// <param name="DailyBudget">Orçamento diário do conjunto.</param>
/// <param name="LifetimeBudget">Orçamento vitalício do conjunto.</param>
/// <param name="TargetingSummary">Resumo de segmentação.</param>
/// <param name="StartDateUtc">Data de início.</param>
/// <param name="EndDateUtc">Data de término.</param>
public sealed record UnifiedAdSetItem(
    string ExternalAdSetId,
    string ExternalCampaignId,
    string Name,
    AdSetStatus Status,
    string? BidStrategy,
    string? OptimizationGoal,
    decimal? DailyBudget,
    decimal? LifetimeBudget,
    string? TargetingSummary,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc);

/// <summary>
/// Representa um anúncio ou criativo normalizado.
/// </summary>
/// <param name="ExternalAdId">Identificador do anúncio na plataforma de origem.</param>
/// <param name="ExternalAdSetId">Identificador do conjunto pai.</param>
/// <param name="ExternalCampaignId">Identificador da campanha associada.</param>
/// <param name="Name">Nome do anúncio.</param>
/// <param name="Status">Status unificado.</param>
/// <param name="CreativeType">Tipo de formato do criativo.</param>
/// <param name="Headline">Título publicitário.</param>
/// <param name="Body">Texto de copy.</param>
/// <param name="DestinationUrl">URL final da Landing Page.</param>
/// <param name="PreviewUrl">URL de prévia do criativo.</param>
/// <param name="CallToAction">Chamada para ação (CTA).</param>
public sealed record UnifiedAdItem(
    string ExternalAdId,
    string ExternalAdSetId,
    string ExternalCampaignId,
    string Name,
    AdStatus Status,
    AdCreativeType CreativeType,
    string? Headline,
    string? Body,
    string? DestinationUrl,
    string? PreviewUrl,
    string? CallToAction);

/// <summary>
/// Contêiner com a hierarquia estrutural completa de campanhas sincronizadas para uma conta.
/// </summary>
/// <param name="Campaigns">Lista de campanhas normalizadas.</param>
/// <param name="AdSets">Lista de conjuntos ou grupos normalizados.</param>
/// <param name="Ads">Lista de anúncios ou criativos normalizados.</param>
public sealed record UnifiedCampaignHierarchy(
    IReadOnlyList<UnifiedCampaignItem> Campaigns,
    IReadOnlyList<UnifiedAdSetItem> AdSets,
    IReadOnlyList<UnifiedAdItem> Ads);
