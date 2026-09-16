using System.Text.Json;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Application.Reports.Commands.DispatchReportSchedule;

/// <summary>
/// Comando para executar e despachar um agendamento de relatório via E-mail e WhatsApp.
/// </summary>
public sealed record DispatchReportScheduleCommand(Guid ScheduleId) : ICommand<Guid>;

/// <summary>
/// Manipulador do comando <see cref="DispatchReportScheduleCommand"/>.
/// </summary>
public sealed class DispatchReportScheduleCommandHandler : ICommandHandler<DispatchReportScheduleCommand, Guid>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReportRepository _repository;
    private readonly IReportDataProvider _dataProvider;
    private readonly IReportPdfGenerator _pdfGenerator;
    private readonly IReportDispatchService _dispatchService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DispatchReportScheduleCommandHandler"/>.
    /// </summary>
    public DispatchReportScheduleCommandHandler(
        IReportRepository repository,
        IReportDataProvider dataProvider,
        IReportPdfGenerator pdfGenerator,
        IReportDispatchService dispatchService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _dispatchService = dispatchService ?? throw new ArgumentNullException(nameof(dispatchService));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(DispatchReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetScheduleByIdAsync(request.ScheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result<Guid>.Failure(Error.NotFound("ReportSchedule.NotFound", "Agendamento de relatório não localizado."));
        }

        var (startUtc, endUtc) = ResolveDateRange(schedule.DateRangeType);

        var modelResult = await _dataProvider.BuildReportModelAsync(
            schedule.WorkspaceId,
            startUtc,
            endUtc,
            schedule.CustomTitle ?? schedule.Name,
            schedule.CustomNotes,
            schedule.IncludeCopilotInsights,
            schedule.IncludeTopCreatives,
            schedule.IncludeChannelBreakdown,
            schedule.IncludePacingSummary,
            cancellationToken);

        if (modelResult.IsFailure)
        {
            return Result<Guid>.Failure(modelResult.Error);
        }

        var model = modelResult.Value;

        // Renderiza PDF se configurado
        byte[]? pdfBytes = null;
        if (schedule.OutputFormat is ReportOutputFormat.Pdf or ReportOutputFormat.Both)
        {
            var pdfResult = await _pdfGenerator.GeneratePdfAsync(model, cancellationToken);
            if (pdfResult.IsSuccess)
            {
                pdfBytes = pdfResult.Value;
            }
        }

        var reportId = Guid.NewGuid();
        var jsonPayload = JsonSerializer.Serialize(model, JsonOptions);
        var expiresAtUtc = DateTime.UtcNow.AddDays(30);

        var reportResult = GeneratedReport.Create(
            reportId,
            schedule.WorkspaceId,
            schedule.Id,
            model.ReportTitle,
            startUtc,
            endUtc,
            expiresAtUtc,
            pdfBytes,
            jsonPayload,
            DateTime.UtcNow);

        if (reportResult.IsFailure)
        {
            return Result<Guid>.Failure(reportResult.Error);
        }

        var report = reportResult.Value;
        await _repository.AddGeneratedReportAsync(report, cancellationToken);

        // Despacha para os destinatários (Email / WhatsApp)
        await _dispatchService.DispatchReportAsync(report, schedule, model, cancellationToken);

        // Atualiza a execução do agendamento
        schedule.RecordExecution(DateTime.UtcNow);
        await _repository.UpdateScheduleAsync(schedule, cancellationToken);

        return Result<Guid>.Success(reportId);
    }

    private static (DateTime start, DateTime end) ResolveDateRange(ReportDateRangeType rangeType)
    {
        var now = DateTime.UtcNow;
        return rangeType switch
        {
            ReportDateRangeType.Last7Days => (now.AddDays(-7).Date, now),
            ReportDateRangeType.Last14Days => (now.AddDays(-14).Date, now),
            ReportDateRangeType.Last30Days => (now.AddDays(-30).Date, now),
            ReportDateRangeType.MonthToDate => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), now),
            ReportDateRangeType.PreviousMonth => (
                new DateTime(now.AddMonths(-1).Year, now.AddMonths(-1).Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1)),
            _ => (now.AddDays(-7).Date, now)
        };
    }
}
