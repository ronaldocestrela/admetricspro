using System.Text.Json;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
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
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GenerateReportCommandHandler"/>.
    /// </summary>
    public GenerateReportCommandHandler(
        IReportDataProvider dataProvider,
        IReportPdfGenerator pdfGenerator,
        IReportRepository repository)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
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

        // Renderiza o PDF com a identidade White-Label
        var pdfResult = await _pdfGenerator.GeneratePdfAsync(model, cancellationToken);
        byte[]? pdfBytes = pdfResult.IsSuccess ? pdfResult.Value : null;

        var reportId = Guid.NewGuid();
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
            pdfContent: pdfBytes,
            reportDataPayloadJson: jsonPayload,
            generatedAtUtc: DateTime.UtcNow);

        if (reportResult.IsFailure)
        {
            return Result<Guid>.Failure(reportResult.Error);
        }

        await _repository.AddGeneratedReportAsync(reportResult.Value, cancellationToken);

        return Result<Guid>.Success(reportId);
    }
}
