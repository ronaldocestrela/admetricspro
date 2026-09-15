namespace Integrations.Application.Campaigns.DTOs;

/// <summary>
/// Projeção da árvore hierárquica de campanha para consumo de APIs e Frontend.
/// </summary>
/// <param name="Id">Identificador único da campanha.</param>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="ConnectedAdAccountId">Identificador da conta conectada.</param>
/// <param name="Platform">Plataforma de mídia.</param>
/// <param name="ExternalCampaignId">ID externo na rede.</param>
/// <param name="Name">Nome da campanha.</param>
/// <param name="Status">Status unificado.</param>
/// <param name="Objective">Objetivo de marketing.</param>
/// <param name="DailyBudget">Orçamento diário.</param>
/// <param name="LifetimeBudget">Orçamento total.</param>
/// <param name="Currency">Moeda da campanha.</param>
/// <param name="StartDateUtc">Data de início da veiculação.</param>
/// <param name="EndDateUtc">Data de término da veiculação.</param>
/// <param name="LastSyncedAtUtc">Carimbo UTC da sincronização.</param>
/// <param name="AdSets">Conjuntos ou grupos de anúncios vinculados.</param>
public sealed record CampaignHierarchyDto(
    Guid Id,
    Guid WorkspaceId,
    Guid ConnectedAdAccountId,
    string Platform,
    string ExternalCampaignId,
    string Name,
    string Status,
    string Objective,
    decimal? DailyBudget,
    decimal? LifetimeBudget,
    string Currency,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    DateTime LastSyncedAtUtc,
    IReadOnlyList<AdSetHierarchyDto> AdSets);

/// <summary>
/// Projeção hierárquica de conjunto ou grupo de anúncios.
/// </summary>
/// <param name="Id">Identificador único do conjunto.</param>
/// <param name="CampaignId">Identificador da campanha pai.</param>
/// <param name="ExternalAdSetId">ID externo na rede.</param>
/// <param name="Name">Nome do conjunto.</param>
/// <param name="Status">Status unificado.</param>
/// <param name="BidStrategy">Estratégia de lances.</param>
/// <param name="OptimizationGoal">Meta de otimização.</param>
/// <param name="DailyBudget">Orçamento diário do conjunto.</param>
/// <param name="LifetimeBudget">Orçamento total do conjunto.</param>
/// <param name="TargetingSummary">Segmentação de público.</param>
/// <param name="Ads">Anúncios ou criativos subordinados.</param>
public sealed record AdSetHierarchyDto(
    Guid Id,
    Guid CampaignId,
    string ExternalAdSetId,
    string Name,
    string Status,
    string? BidStrategy,
    string? OptimizationGoal,
    decimal? DailyBudget,
    decimal? LifetimeBudget,
    string? TargetingSummary,
    IReadOnlyList<AdHierarchyDto> Ads);

/// <summary>
/// Projeção hierárquica de anúncio ou criativo.
/// </summary>
/// <param name="Id">Identificador único do anúncio.</param>
/// <param name="AdSetId">Identificador do conjunto pai.</param>
/// <param name="ExternalAdId">ID externo na rede.</param>
/// <param name="Name">Nome do anúncio.</param>
/// <param name="Status">Status unificado.</param>
/// <param name="CreativeType">Formato do criativo.</param>
/// <param name="Headline">Título publicitário.</param>
/// <param name="Body">Texto de copy.</param>
/// <param name="DestinationUrl">URL final de destino.</param>
/// <param name="PreviewUrl">URL de prévia da imagem ou vídeo.</param>
/// <param name="CallToAction">Chamada para ação.</param>
public sealed record AdHierarchyDto(
    Guid Id,
    Guid AdSetId,
    string ExternalAdId,
    string Name,
    string Status,
    string CreativeType,
    string? Headline,
    string? Body,
    string? DestinationUrl,
    string? PreviewUrl,
    string? CallToAction);
