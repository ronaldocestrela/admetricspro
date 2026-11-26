using Analytics.Application.Reports.Commands.GenerateReport;
using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários para <see cref="GenerateReportCommandHandler"/>.
/// </summary>
public sealed class GenerateReportCommandHandlerTests
{
    private readonly IReportDataProvider _dataProvider = Substitute.For<IReportDataProvider>();
    private readonly IReportPdfGenerator _pdfGenerator = Substitute.For<IReportPdfGenerator>();
    private readonly IReportPdfStore _pdfStore = Substitute.For<IReportPdfStore>();
    private readonly IReportRepository _repository = Substitute.For<IReportRepository>();
    private readonly GenerateReportCommandHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes.
    /// </summary>
    public GenerateReportCommandHandlerTests()
    {
        _handler = new GenerateReportCommandHandler(_dataProvider, _pdfGenerator, _pdfStore, _repository);
    }

    /// <summary>
    /// Valida que a geração sob demanda compila o modelo, produz o PDF, salva o relatório gerado e retorna o Guid.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRequest_ShouldBuildModelGeneratePdfAndSaveReport()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(-14);
        var end = DateTime.UtcNow;

        var renderModel = new ReportRenderModel
        {
            ReportTitle = "Relatório Quinzenal Sob Demanda",
            WorkspaceName = "Cliente VIP",
            DateRangeStart = start,
            DateRangeEnd = end,
            Branding = ReportBrandingSnapshot.Default,
            KpiSummary = new ReportKpiSummary { TotalSpend = 5000m, TotalRevenue = 20000m, BlendedRoas = 4.0m }
        };

        _dataProvider.BuildReportModelAsync(
            workspaceId, start, end, "Relatório Quinzenal", "Observações", true, true, true, true, Arg.Any<CancellationToken>())
            .Returns(Result<ReportRenderModel>.Success(renderModel));

        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _pdfGenerator.GeneratePdfAsync(renderModel, Arg.Any<CancellationToken>())
            .Returns(Result<byte[]>.Success(pdfBytes));

        const string objectKey = "tenant/ws/report.pdf";
        _pdfStore.SaveAsync(workspaceId, Arg.Any<Guid>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(objectKey));

        _repository.AddGeneratedReportAsync(Arg.Any<GeneratedReport>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new GenerateReportCommand(
            workspaceId,
            "Relatório Quinzenal",
            start,
            end,
            "Observações",
            IncludeCopilotInsights: true,
            IncludeTopCreatives: true,
            IncludeChannelBreakdown: true,
            IncludePacingSummary: true,
            ExpirationDays: 30);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _repository.Received(1).AddGeneratedReportAsync(
            Arg.Is<GeneratedReport>(r => r.WorkspaceId == workspaceId && r.PdfObjectKey == objectKey && r.PdfContent == null),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha de gravação no object storage não persiste o relatório.
    /// </summary>
    [Fact]
    public async Task Handle_WhenPdfStoreFails_ShouldNotPersistReport()
    {
        var workspaceId = Guid.NewGuid();
        var renderModel = new ReportRenderModel
        {
            ReportTitle = "Relatório",
            WorkspaceName = "Cliente",
            DateRangeStart = DateTime.UtcNow.AddDays(-7),
            DateRangeEnd = DateTime.UtcNow,
            Branding = ReportBrandingSnapshot.Default,
            KpiSummary = new ReportKpiSummary(),
        };

        _dataProvider.BuildReportModelAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReportRenderModel>.Success(renderModel));

        _pdfGenerator.GeneratePdfAsync(renderModel, Arg.Any<CancellationToken>())
            .Returns(Result<byte[]>.Success(new byte[] { 1, 2, 3 }));

        _pdfStore.SaveAsync(workspaceId, Arg.Any<Guid>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(BuildingBlocks.Application.Storage.StorageErrors.WriteFailed));

        var command = new GenerateReportCommand(workspaceId, "T", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(BuildingBlocks.Application.Storage.StorageErrors.WriteFailed.Code);
        await _repository.DidNotReceive().AddGeneratedReportAsync(Arg.Any<GeneratedReport>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que se a coleta de dados de métricas falhar, o handler retorna a falha correspondente.
    /// </summary>
    [Fact]
    public async Task Handle_WhenDataProviderFails_ShouldReturnFailure()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        _dataProvider.BuildReportModelAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReportRenderModel>.Failure(Error.NotFound("Workspace.NotFound", "Workspace inexistente.")));

        var command = new GenerateReportCommand(
            workspaceId, "Relatório", DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.NotFound");
        await _repository.DidNotReceive().AddGeneratedReportAsync(Arg.Any<GeneratedReport>(), Arg.Any<CancellationToken>());
    }
}
