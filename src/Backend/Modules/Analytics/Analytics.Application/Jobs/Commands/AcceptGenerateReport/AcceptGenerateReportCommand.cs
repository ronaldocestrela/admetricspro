using BuildingBlocks.Application.Messaging;
using Analytics.Application.Reports.Commands.GenerateReport;

namespace Analytics.Application.Jobs.Commands.AcceptGenerateReport;

/// <summary>
/// Aceita compilação assíncrona de relatório executivo.
/// </summary>
public sealed record AcceptGenerateReportCommand(
    Guid WorkspaceId,
    string? Title,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string? CustomNotes,
    bool IncludeCopilotInsights = true,
    bool IncludeTopCreatives = true,
    bool IncludeChannelBreakdown = true,
    bool IncludePacingSummary = true,
    int ExpirationDays = 30) : ICommand<UserInitiatedJobDto>;