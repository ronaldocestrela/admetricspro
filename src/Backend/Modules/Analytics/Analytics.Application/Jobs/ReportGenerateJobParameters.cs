namespace Analytics.Application.Jobs;

/// <summary>
/// Parâmetros mínimos para compilação de relatório sob demanda.
/// </summary>
public sealed record ReportGenerateJobParameters(
    Guid WorkspaceId,
    string? Title,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string? CustomNotes,
    bool IncludeCopilotInsights,
    bool IncludeTopCreatives,
    bool IncludeChannelBreakdown,
    bool IncludePacingSummary,
    int ExpirationDays);