using Analytics.Application.Copilot.DTOs;
using Analytics.Domain.Copilot;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Copilot.Queries.GetDailyDiagnostic;

/// <summary>
/// Manipulador da consulta <see cref="GetDailyDiagnosticQuery"/>.
/// Orquestra a extração de dados do banco dedicado do inquilino, aciona os detectores algorítmicos e sintetiza o diagnóstico em texto natural.
/// </summary>
public sealed class GetDailyDiagnosticQueryHandler : IQueryHandler<GetDailyDiagnosticQuery, DailyDiagnosticReportDto>
{
    private readonly ICopilotDataProvider _dataProvider;
    private readonly IAudienceOverlapDetector _overlapDetector;
    private readonly ISearchTermCannibalizationDetector _cannibalizationDetector;
    private readonly ITrafficAuditorSynthesizer _synthesizer;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetDailyDiagnosticQueryHandler"/>.
    /// </summary>
    public GetDailyDiagnosticQueryHandler(
        ICopilotDataProvider dataProvider,
        IAudienceOverlapDetector overlapDetector,
        ISearchTermCannibalizationDetector cannibalizationDetector,
        ITrafficAuditorSynthesizer synthesizer)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _overlapDetector = overlapDetector ?? throw new ArgumentNullException(nameof(overlapDetector));
        _cannibalizationDetector = cannibalizationDetector ?? throw new ArgumentNullException(nameof(cannibalizationDetector));
        _synthesizer = synthesizer ?? throw new ArgumentNullException(nameof(synthesizer));
    }

    /// <inheritdoc />
    public async Task<Result<DailyDiagnosticReportDto>> Handle(
        GetDailyDiagnosticQuery query,
        CancellationToken cancellationToken)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            return Result<DailyDiagnosticReportDto>.Failure(
                Error.Validation("Copilot.InvalidWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        var reportDate = (query.Date ?? DateTime.UtcNow).Date;
        var startDate = reportDate.AddDays(-7);
        var endDate = reportDate;

        var metaResult = await _dataProvider.GetMetaAdSetsForAuditAsync(query.WorkspaceId, startDate, endDate, cancellationToken);
        if (metaResult.IsFailure)
        {
            return Result<DailyDiagnosticReportDto>.Failure(metaResult.Error);
        }

        var searchResult = await _dataProvider.GetSearchKeywordsForAuditAsync(query.WorkspaceId, startDate, endDate, cancellationToken);
        if (searchResult.IsFailure)
        {
            return Result<DailyDiagnosticReportDto>.Failure(searchResult.Error);
        }

        var overlapAnomalies = _overlapDetector.DetectOverlaps(metaResult.Value);
        var cannibalizationAnomalies = _cannibalizationDetector.DetectCannibalization(searchResult.Value);

        var report = _synthesizer.SynthesizeDailyDiagnostic(
            query.WorkspaceId,
            reportDate,
            overlapAnomalies,
            cannibalizationAnomalies);

        var dto = MapToDto(report);
        return Result<DailyDiagnosticReportDto>.Success(dto);
    }

    private static DailyDiagnosticReportDto MapToDto(DailyDiagnosticReport report)
    {
        var actionsDto = report.Actions.Select(MapActionToDto).ToList();

        var overlapDtos = report.AudienceOverlapAnomalies.Select(a => new AudienceOverlapAnomalyDto(
            a.AdSetIdA,
            a.AdSetNameA,
            a.CampaignIdA,
            a.CampaignNameA,
            a.AdSetIdB,
            a.AdSetNameB,
            a.CampaignIdB,
            a.CampaignNameB,
            a.OverlapPercentage,
            a.CpmA,
            a.CpmB,
            a.Severity.ToString(),
            a.Description,
            MapActionToDto(a.SuggestedAction),
            a.SharedTargetingTags
        )).ToList();

        var cannibalizationDtos = report.SearchCannibalizationAnomalies.Select(c => new SearchCannibalizationAnomalyDto(
            c.SearchTerm,
            c.ChannelA,
            c.CampaignIdA,
            c.CampaignNameA,
            c.CpcA,
            c.CpaA,
            c.SpendA,
            c.ChannelB,
            c.CampaignIdB,
            c.CampaignNameB,
            c.CpcB,
            c.CpaB,
            c.SpendB,
            c.DisparityRatio,
            c.EstimatedMonthlyWastedSpend,
            c.Severity.ToString(),
            c.Description,
            MapActionToDto(c.SuggestedAction)
        )).ToList();

        return new DailyDiagnosticReportDto(
            report.WorkspaceId,
            report.ReportDate,
            report.ExecutiveSummary,
            report.WinsSummary,
            report.RisksSummary,
            overlapDtos,
            cannibalizationDtos,
            actionsDto,
            report.EstimatedMonthlySavings,
            report.CriticalAnomaliesCount,
            report.HighAnomaliesCount
        );
    }

    private static CopilotRecommendationActionDto MapActionToDto(CopilotRecommendationAction action)
    {
        return new CopilotRecommendationActionDto(
            action.ActionId,
            action.ActionType.ToString(),
            action.TargetEntityId,
            action.TargetEntityName,
            action.Platform,
            action.Title,
            action.Description,
            action.Parameters,
            action.IsApplied,
            action.AppliedAtUtc
        );
    }
}
