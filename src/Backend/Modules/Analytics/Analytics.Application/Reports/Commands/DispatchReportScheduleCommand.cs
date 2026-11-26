using System.Text.Json;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Storage;
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
    private readonly IReportPdfStore _pdfStore;
    private readonly IReportDispatchService _dispatchService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DispatchReportScheduleCommandHandler"/>.
    /// </summary>
    public DispatchReportScheduleCommandHandler(
        IReportRepository repository,
        IReportDataProvider dataProvider,
        IReportPdfGenerator pdfGenerator,
        IReportPdfStore pdfStore,
        IReportDispatchService dispatchService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _pdfStore = pdfStore ?? throw new ArgumentNullException(nameof(pdfStore));
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
        var reportId = Guid.NewGuid();
        string? pdfObjectKey = null;

        if (schedule.OutputFormat is ReportOutputFormat.Pdf or ReportOutputFormat.Both)
        {
            var pdfResult = await _pdfGenerator.GeneratePdfAsync(model, cancellationToken);
            if (pdfResult.IsSuccess && pdfResult.Value is { Length: > 0 })
            {
                await using var stream = new MemoryStream(pdfResult.Value);
                var saveResult = await _pdfStore.SaveAsync(schedule.WorkspaceId, reportId, stream, cancellationToken);
                if (saveResult.IsFailure)
                {
                    return Result<Guid>.Failure(saveResult.Error);
                }

                pdfObjectKey = saveResult.Value;
            }
        }

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
            pdfContent: null,
            reportDataPayloadJson: jsonPayload,
            generatedAtUtc: DateTime.UtcNow,
            pdfObjectKey: pdfObjectKey);

        if (reportResult.IsFailure)
        {
            if (pdfObjectKey is not null)
            {
                await _pdfStore.DeleteAsync(pdfObjectKey, cancellationToken);
            }

            return Result<Guid>.Failure(reportResult.Error);
        }

        var report = reportResult.Value;
        var addResult = await _repository.AddGeneratedReportAsync(report, cancellationToken);
        if (addResult.IsFailure)
        {
            if (pdfObjectKey is not null)
            {
                await _pdfStore.DeleteAsync(pdfObjectKey, cancellationToken);
            }

            return Result<Guid>.Failure(addResult.Error.Code == StorageErrors.WriteFailed.Code
                ? StorageErrors.WriteFailed
                : addResult.Error);
        }

        await _dispatchService.DispatchReportAsync(report, schedule, model, cancellationToken);

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