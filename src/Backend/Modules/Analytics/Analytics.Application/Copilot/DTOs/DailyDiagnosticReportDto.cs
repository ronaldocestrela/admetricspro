using Analytics.Domain.Copilot;

namespace Analytics.Application.Copilot.DTOs;

/// <summary>
/// DTO representando uma ação recomendada pelo Copiloto pronta para execução em 1 clique.
/// </summary>
public sealed record CopilotRecommendationActionDto(
    Guid ActionId,
    string ActionType,
    Guid TargetEntityId,
    string TargetEntityName,
    string Platform,
    string Title,
    string Description,
    IReadOnlyDictionary<string, string> Parameters,
    bool IsApplied,
    DateTime? AppliedAtUtc);

/// <summary>
/// DTO representando uma anomalia de sobreposição de públicos no Meta Ads.
/// </summary>
public sealed record AudienceOverlapAnomalyDto(
    Guid AdSetIdA,
    string AdSetNameA,
    Guid CampaignIdA,
    string CampaignNameA,
    Guid AdSetIdB,
    string AdSetNameB,
    Guid CampaignIdB,
    string CampaignNameB,
    decimal OverlapPercentage,
    decimal CpmA,
    decimal CpmB,
    string Severity,
    string Description,
    CopilotRecommendationActionDto SuggestedAction,
    IReadOnlyList<string> SharedTargetingTags);

/// <summary>
/// DTO representando uma anomalia de canibalização e disputa de termos de busca.
/// </summary>
public sealed record SearchCannibalizationAnomalyDto(
    string SearchTerm,
    string ChannelA,
    Guid CampaignIdA,
    string CampaignNameA,
    decimal CpcA,
    decimal CpaA,
    decimal SpendA,
    string ChannelB,
    Guid CampaignIdB,
    string CampaignNameB,
    decimal CpcB,
    decimal CpaB,
    decimal SpendB,
    decimal DisparityRatio,
    decimal EstimatedMonthlyWastedSpend,
    string Severity,
    string Description,
    CopilotRecommendationActionDto SuggestedAction);

/// <summary>
/// DTO consolidando o relatório de diagnóstico diário emitido pelo Copiloto de IA.
/// </summary>
public sealed record DailyDiagnosticReportDto(
    Guid WorkspaceId,
    DateTime ReportDate,
    string ExecutiveSummary,
    string WinsSummary,
    string RisksSummary,
    IReadOnlyList<AudienceOverlapAnomalyDto> AudienceOverlapAnomalies,
    IReadOnlyList<SearchCannibalizationAnomalyDto> SearchCannibalizationAnomalies,
    IReadOnlyList<CopilotRecommendationActionDto> Actions,
    decimal EstimatedMonthlySavings,
    int CriticalAnomaliesCount,
    int HighAnomaliesCount);

/// <summary>
/// DTO de retorno para a execução em 1 clique de uma recomendação do Copiloto.
/// </summary>
public sealed record ExecuteCopilotActionResultDto(
    Guid ActionId,
    bool Success,
    string Message,
    DateTime ExecutedAtUtc);
