using System.Text.Json;
using Analytics.Application.Reports;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Application.Reports.Commands.GenerateReport;

/// <summary>
/// Comando para compilar e gerar um relatório executivo white-label sob demanda.
/// </summary>
public sealed record GenerateReportCommand(
    Guid WorkspaceId,
    string? Title,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string? CustomNotes,
    bool IncludeCopilotInsights = true,
    bool IncludeTopCreatives = true,
    bool IncludeChannelBreakdown = true,
    bool IncludePacingSummary = true,
    int ExpirationDays = 30) : ICommand<Guid>;

/// <summary>
/// Manipulador do comando <see cref="GenerateReportCommand"/>.
/// </summary>
public sealed class GenerateReportCommandHandler : ICommandHandler<GenerateReportCommand, Guid>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReportDataProvider _dataProvider;
    private readonly IReportPdfGenerator _pdfGenerator;
    private readonly IReportPdfStore _pdfStore;
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GenerateReportCommandHandler"/>.
    /// </summary>
    public GenerateReportCommandHandler(
        IReportDataProvider dataProvider,
        IReportPdfGenerator pdfGenerator,
        IReportPdfStore pdfStore,
        IReportRepository repository)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _pdfStore = pdfStore ?? throw new ArgumentNullException(nameof(pdfStore));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        var modelResult = await _dataProvider.BuildReportModelAsync(
            request.WorkspaceId,
            request.StartDateUtc,
            request.EndDateUtc,
            request.Title,
            request.CustomNotes,
            request.IncludeCopilotInsights,
            request.IncludeTopCreatives,
            request.IncludeChannelBreakdown,
            request.IncludePacingSummary,
            cancellationToken);

        if (modelResult.IsFailure)
        {
            return Result<Guid>.Failure(modelResult.Error);
        }

        var model = modelResult.Value;
        var pdfResult = await _pdfGenerator.GeneratePdfAsync(model, cancellationToken);

        var reportId = Guid.NewGuid();
        string? pdfObjectKey = null;

        if (pdfResult.IsSuccess && pdfResult.Value is { Length: > 0 })
        {
            await using var stream = new MemoryStream(pdfResult.Value);
            var saveResult = await _pdfStore.SaveAsync(request.WorkspaceId, reportId, stream, cancellationToken);
            if (saveResult.IsFailure)
            {
                return Result<Guid>.Failure(saveResult.Error);
            }

            pdfObjectKey = saveResult.Value;
        }

        var expiresAtUtc = DateTime.UtcNow.AddDays(request.ExpirationDays > 0 ? request.ExpirationDays : 30);
        var jsonPayload = JsonSerializer.Serialize(model, JsonOptions);

        var reportResult = GeneratedReport.Create(
            reportId,
            request.WorkspaceId,
            reportScheduleId: null,
            title: model.ReportTitle,
            dateRangeStartUtc: request.StartDateUtc,
            dateRangeEndUtc: request.EndDateUtc,
            shareTokenExpiresAtUtc: expiresAtUtc,
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

        var addResult = await _repository.AddGeneratedReportAsync(reportResult.Value, cancellationToken);
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

        return Result<Guid>.Success(reportId);
    }
}