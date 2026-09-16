using System.Text.Json;
using Analytics.Application.Reports.DTOs;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Application.Reports.Queries;

/// <summary>
/// Consulta para listar as regras de agendamento de relatórios de um Workspace.
/// </summary>
public sealed record GetReportSchedulesQuery(Guid WorkspaceId) : IQuery<IReadOnlyList<ReportScheduleDto>>;

/// <summary>
/// Manipulador da consulta <see cref="GetReportSchedulesQuery"/>.
/// </summary>
public sealed class GetReportSchedulesQueryHandler : IQueryHandler<GetReportSchedulesQuery, IReadOnlyList<ReportScheduleDto>>
{
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetReportSchedulesQueryHandler"/>.
    /// </summary>
    public GetReportSchedulesQueryHandler(IReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ReportScheduleDto>>> Handle(GetReportSchedulesQuery request, CancellationToken cancellationToken)
    {
        var schedules = await _repository.GetSchedulesByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);

        var dtos = schedules.Select(s => new ReportScheduleDto(
            s.Id,
            s.WorkspaceId,
            s.Name,
            s.Frequency,
            s.DayOfWeek,
            s.DayOfMonth,
            s.ScheduledTimeUtc,
            s.DateRangeType,
            s.OutputFormat,
            s.DeliveryChannels,
            s.CustomTitle,
            s.CustomNotes,
            s.IncludeCopilotInsights,
            s.IncludeTopCreatives,
            s.IncludeChannelBreakdown,
            s.IncludePacingSummary,
            s.IsActive,
            s.LastExecutedAtUtc,
            s.NextExecutionAtUtc,
            s.CreatedAtUtc,
            s.Recipients.Select(r => new ReportRecipientDto(r.Name, r.Channel, r.Destination)).ToList()))
            .ToList();

        return Result<IReadOnlyList<ReportScheduleDto>>.Success(dtos);
    }
}

/// <summary>
/// Consulta para listar os relatórios gerados e histórico de um Workspace.
/// </summary>
public sealed record GetGeneratedReportsQuery(Guid WorkspaceId, int Limit = 50) : IQuery<IReadOnlyList<GeneratedReportSummaryDto>>;

/// <summary>
/// Manipulador da consulta <see cref="GetGeneratedReportsQuery"/>.
/// </summary>
public sealed class GetGeneratedReportsQueryHandler : IQueryHandler<GetGeneratedReportsQuery, IReadOnlyList<GeneratedReportSummaryDto>>
{
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetGeneratedReportsQueryHandler"/>.
    /// </summary>
    public GetGeneratedReportsQueryHandler(IReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GeneratedReportSummaryDto>>> Handle(GetGeneratedReportsQuery request, CancellationToken cancellationToken)
    {
        var reports = await _repository.GetReportsByWorkspaceIdAsync(request.WorkspaceId, request.Limit, cancellationToken);

        var dtos = reports.Select(r => new GeneratedReportSummaryDto(
            r.Id,
            r.WorkspaceId,
            r.ReportScheduleId,
            r.Title,
            r.DateRangeStartUtc,
            r.DateRangeEndUtc,
            r.ShareToken,
            r.ShareTokenExpiresAtUtc,
            r.Status,
            r.GeneratedAtUtc,
            ShareUrl: $"/reports/shared/{r.ShareToken}",
            HasPdf: r.PdfContent is not null && r.PdfContent.Length > 0))
            .ToList();

        return Result<IReadOnlyList<GeneratedReportSummaryDto>>.Success(dtos);
    }
}

/// <summary>
/// Consulta pública por token de compartilhamento para visualização interativa do relatório.
/// </summary>
public sealed record GetPublicSharedReportQuery(string ShareToken) : IQuery<PublicSharedReportDto>;

/// <summary>
/// Manipulador da consulta <see cref="GetPublicSharedReportQuery"/>.
/// </summary>
public sealed class GetPublicSharedReportQueryHandler : IQueryHandler<GetPublicSharedReportQuery, PublicSharedReportDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetPublicSharedReportQueryHandler"/>.
    /// </summary>
    public GetPublicSharedReportQueryHandler(IReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<PublicSharedReportDto>> Handle(GetPublicSharedReportQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ShareToken))
        {
            return Result<PublicSharedReportDto>.Failure(Error.Validation("Report.InvalidToken", "O token do relatório é inválido."));
        }

        var report = await _repository.GetReportByShareTokenAsync(request.ShareToken, cancellationToken);
        if (report is null)
        {
            return Result<PublicSharedReportDto>.Failure(Error.NotFound("Report.NotFound", "Relatório não localizado ou link inválido."));
        }

        var isExpired = report.IsExpired(DateTime.UtcNow);

        // Desserializa o modelo
        var model = JsonSerializer.Deserialize<ReportRenderModel>(report.ReportDataPayloadJson, JsonOptions);
        if (model is null)
        {
            return Result<PublicSharedReportDto>.Failure(Error.Failure("Report.CorruptedData", "Dados do relatório corrompidos."));
        }

        var brandingDto = new ReportBrandingDto(
            model.Branding?.PrimaryColor ?? "#2563EB",
            model.Branding?.SecondaryColor ?? "#0F172A",
            model.Branding?.LightLogoUrl,
            model.Branding?.DarkLogoUrl,
            model.Branding?.FaviconUrl,
            model.Branding?.AgencyName ?? "Agência de Performance",
            model.Branding?.SupportEmail,
            model.Branding?.SupportPhone,
            model.Branding?.CustomDomain);

        var dto = new PublicSharedReportDto(
            report.Title,
            model.WorkspaceName,
            report.DateRangeStartUtc,
            report.DateRangeEndUtc,
            brandingDto,
            model.KpiSummary,
            model.ChannelBreakdown,
            model.TopCreatives,
            model.CopilotInsights,
            model.CustomNotes,
            report.GeneratedAtUtc,
            isExpired,
            report.ShareToken);

        return Result<PublicSharedReportDto>.Success(dto);
    }
}

/// <summary>
/// Consulta para obter os bytes do PDF de um relatório gerado.
/// </summary>
public sealed record GetReportPdfQuery(Guid? ReportId, string? ShareToken) : IQuery<byte[]>;

/// <summary>
/// Manipulador da consulta <see cref="GetReportPdfQuery"/>.
/// </summary>
public sealed class GetReportPdfQueryHandler : IQueryHandler<GetReportPdfQuery, byte[]>
{
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetReportPdfQueryHandler"/>.
    /// </summary>
    public GetReportPdfQueryHandler(IReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> Handle(GetReportPdfQuery request, CancellationToken cancellationToken)
    {
        GeneratedReport? report = null;

        if (request.ReportId.HasValue)
        {
            report = await _repository.GetReportByIdAsync(request.ReportId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.ShareToken))
        {
            report = await _repository.GetReportByShareTokenAsync(request.ShareToken, cancellationToken);
        }

        if (report is null)
        {
            return Result<byte[]>.Failure(Error.NotFound("Report.NotFound", "Relatório não localizado."));
        }

        if (report.PdfContent is null || report.PdfContent.Length == 0)
        {
            return Result<byte[]>.Failure(Error.NotFound("Report.PdfNotFound", "Arquivo PDF deste relatório não foi gerado."));
        }

        return Result<byte[]>.Success(report.PdfContent);
    }
}
