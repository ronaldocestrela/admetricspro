namespace Analytics.Application.Creatives.DTOs;

/// <summary>
/// DTO que representa o resultado da análise de fadiga e saturação de um criativo publicitário.
/// </summary>
public sealed record CreativeFatigueDto(
    Guid AdId,
    string AdName,
    string Platform,
    string? PreviewUrl,
    string Status,
    int AnalyzedDays,
    decimal InitialCtr,
    decimal CurrentCtr,
    decimal CtrChangePercentage,
    decimal CtrTrendSlope,
    decimal AverageFrequency,
    decimal CurrentFrequency,
    bool ReplacementSuggested,
    string Reason,
    string ActionRecommendation);

/// <summary>
/// DTO com métricas consolidadas de desempenho de um criativo em uma rede de anúncios específica.
/// </summary>
public sealed record CrossPlatformMetricsDto(
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
/// DTO com comparativo cross-platform do mesmo criativo entre Meta Ads e TikTok Ads.
/// </summary>
public sealed record CrossPlatformComparisonDto(
    string AssetFingerprint,
    string AssetName,
    string? PreviewUrl,
    CrossPlatformMetricsDto MetaMetrics,
    CrossPlatformMetricsDto TikTokMetrics,
    string WinningPlatform,
    decimal CpaDifferencePercentage,
    decimal CtrDifferencePercentage,
    string EfficiencySummary);

/// <summary>
/// DTO de ativo orgânico registrado no Creative Hub por tração de publicação social.
/// </summary>
/// <param name="Id">Identificador do registro no hub.</param>
/// <param name="OrganicPostId">Post orgânico de origem.</param>
/// <param name="Network">Rede social de origem.</param>
/// <param name="Permalink">URL pública do post, quando conhecida.</param>
/// <param name="EngagementMultiple">Múltiplo de engajamento sobre a mediana na detecção.</param>
public sealed record CreativeHubOrganicAssetDto(
    Guid Id,
    Guid OrganicPostId,
    string Network,
    string? Permalink,
    decimal EngagementMultiple);

/// <summary>
/// DTO consolidado para visão geral do Creative Hub do workspace.
/// </summary>
public sealed record CreativeHubOverviewDto(
    Guid WorkspaceId,
    int TotalCreatives,
    int FatiguedCreativesCount,
    int WarningCreativesCount,
    int HealthyCreativesCount,
    int ReplacementsSuggestedCount,
    IReadOnlyList<CreativeFatigueDto> Creatives,
    IReadOnlyList<CreativeHubOrganicAssetDto>? OrganicAssets = null);
