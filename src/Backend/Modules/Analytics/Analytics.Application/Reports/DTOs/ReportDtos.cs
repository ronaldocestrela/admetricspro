using BuildingBlocks.Domain.Reports;

namespace Analytics.Application.Reports.DTOs;

/// <summary>
/// DTO de destinatário configurado para receber relatórios.
/// </summary>
public sealed record ReportRecipientDto(
    string Name,
    ReportDeliveryChannel Channel,
    string Destination);

/// <summary>
/// DTO completo de regra de agendamento de relatórios.
/// </summary>
public sealed record ReportScheduleDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    ReportFrequency Frequency,
    DayOfWeek? DayOfWeek,
    int? DayOfMonth,
    TimeSpan ScheduledTimeUtc,
    ReportDateRangeType DateRangeType,
    ReportOutputFormat OutputFormat,
    ReportDeliveryChannel DeliveryChannels,
    string? CustomTitle,
    string? CustomNotes,
    bool IncludeCopilotInsights,
    bool IncludeTopCreatives,
    bool IncludeChannelBreakdown,
    bool IncludePacingSummary,
    bool IsActive,
    DateTime? LastExecutedAtUtc,
    DateTime? NextExecutionAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<ReportRecipientDto> Recipients);

/// <summary>
/// DTO resumido de relatório compilado no histórico.
/// </summary>
public sealed record GeneratedReportSummaryDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? ReportScheduleId,
    string Title,
    DateTime DateRangeStartUtc,
    DateTime DateRangeEndUtc,
    string ShareToken,
    DateTime? ShareTokenExpiresAtUtc,
    ReportStatus Status,
    DateTime GeneratedAtUtc,
    string ShareUrl,
    bool HasPdf);

/// <summary>
/// DTO de identidade visual da agência para apresentação do relatório white-label.
/// </summary>
public sealed record ReportBrandingDto(
    string PrimaryColor,
    string SecondaryColor,
    string? LightLogoUrl,
    string? DarkLogoUrl,
    string? FaviconUrl,
    string AgencyName,
    string? SupportEmail,
    string? SupportPhone,
    string? CustomDomain);

/// <summary>
/// DTO público consumido pelo link web interativo via share token.
/// </summary>
public sealed record PublicSharedReportDto(
    string ReportTitle,
    string WorkspaceName,
    DateTime DateRangeStart,
    DateTime DateRangeEnd,
    ReportBrandingDto Branding,
    Domain.Reports.ReportKpiSummary KpiSummary,
    IReadOnlyList<Domain.Reports.ReportChannelMetric> ChannelBreakdown,
    IReadOnlyList<Domain.Reports.ReportTopCreative> TopCreatives,
    IReadOnlyList<string> CopilotInsights,
    string? CustomNotes,
    DateTime GeneratedAt,
    bool IsExpired,
    string? ShareToken);

/// <summary>
/// Payload para criação de uma nova regra de agendamento.
/// </summary>
public sealed record CreateReportScheduleRequest(
    string Name,
    ReportFrequency Frequency,
    DayOfWeek? DayOfWeek,
    int? DayOfMonth,
    TimeSpan ScheduledTimeUtc,
    ReportDateRangeType DateRangeType,
    ReportOutputFormat OutputFormat,
    ReportDeliveryChannel DeliveryChannels,
    string? CustomTitle,
    string? CustomNotes,
    bool IncludeCopilotInsights,
    bool IncludeTopCreatives,
    bool IncludeChannelBreakdown,
    bool IncludePacingSummary,
    List<ReportRecipientDto>? Recipients);

/// <summary>
/// Payload para geração imediata sob demanda de um relatório executivo.
/// </summary>
public sealed record GenerateReportRequest(
    string? Title,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string? CustomNotes,
    bool IncludeCopilotInsights = true,
    bool IncludeTopCreatives = true,
    bool IncludeChannelBreakdown = true,
    bool IncludePacingSummary = true,
    int ExpirationDays = 30);
