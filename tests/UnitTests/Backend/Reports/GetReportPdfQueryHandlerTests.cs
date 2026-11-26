using Analytics.Application.Reports;
using Analytics.Application.Reports.Queries;
using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes de leitura de PDF com coexistência objeto/coluna legada.
/// </summary>
public sealed class GetReportPdfQueryHandlerTests
{
    private readonly IReportRepository _repository = Substitute.For<IReportRepository>();
    private readonly IReportPdfStore _pdfStore = Substitute.For<IReportPdfStore>();
    private readonly GetReportPdfQueryHandler _handler;

    /// <summary>
    /// Inicializa o handler sob teste.
    /// </summary>
    public GetReportPdfQueryHandlerTests()
    {
        _handler = new GetReportPdfQueryHandler(_repository, new ReportPdfContentReader(_pdfStore));
    }

    /// <summary>
    /// Com apenas <c>PdfObjectKey</c>, lê do store.
    /// </summary>
    [Fact]
    public async Task Handle_ComApenasObjectKey_DeveLerDoStore()
    {
        var reportId = Guid.NewGuid();
        var key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb/cccccccccccccccccccccccccccccccccc.pdf";
        var bytes = new byte[] { 1, 2, 3 };

        var report = GeneratedReport.Create(
            reportId,
            Guid.NewGuid(),
            null,
            "Título",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            null,
            pdfContent: null,
            reportDataPayloadJson: "{}",
            generatedAtUtc: DateTime.UtcNow,
            pdfObjectKey: key).Value;

        _repository.GetReportByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);
        _pdfStore.OpenReadAsync(key, Arg.Any<CancellationToken>())
            .Returns(Result<Stream>.Success(new MemoryStream(bytes)));

        var result = await _handler.Handle(new GetReportPdfQuery(reportId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(bytes);
    }

    /// <summary>
    /// Com apenas coluna legada, devolve <c>PdfContent</c>.
    /// </summary>
    [Fact]
    public async Task Handle_ComApenasPdfContent_DeveRetornarColuna()
    {
        var reportId = Guid.NewGuid();
        var bytes = new byte[] { 9, 8, 7 };

        var report = GeneratedReport.Create(
            reportId,
            Guid.NewGuid(),
            null,
            "Título",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            null,
            pdfContent: bytes,
            reportDataPayloadJson: "{}",
            generatedAtUtc: DateTime.UtcNow).Value;

        _repository.GetReportByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);

        var result = await _handler.Handle(new GetReportPdfQuery(reportId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(bytes);
        await _pdfStore.DidNotReceive().OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Com chave e coluna, prioriza o objeto.
    /// </summary>
    [Fact]
    public async Task Handle_ComChaveEColuna_DevePriorizarObjeto()
    {
        var reportId = Guid.NewGuid();
        var key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb/cccccccccccccccccccccccccccccccccc.pdf";
        var objectBytes = new byte[] { 5, 5, 5 };
        var columnBytes = new byte[] { 9, 9, 9 };

        var report = GeneratedReport.Create(
            reportId,
            Guid.NewGuid(),
            null,
            "Título",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            null,
            pdfContent: columnBytes,
            reportDataPayloadJson: "{}",
            generatedAtUtc: DateTime.UtcNow,
            pdfObjectKey: key).Value;

        _repository.GetReportByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);
        _pdfStore.OpenReadAsync(key, Arg.Any<CancellationToken>())
            .Returns(Result<Stream>.Success(new MemoryStream(objectBytes)));

        var result = await _handler.Handle(new GetReportPdfQuery(reportId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(objectBytes);
    }
}