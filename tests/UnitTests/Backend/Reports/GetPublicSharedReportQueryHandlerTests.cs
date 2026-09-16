using System.Text.Json;
using Analytics.Application.Reports.Queries;
using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários para <see cref="GetPublicSharedReportQueryHandler"/>.
/// </summary>
public sealed class GetPublicSharedReportQueryHandlerTests
{
    private readonly IReportRepository _repository = Substitute.For<IReportRepository>();
    private readonly GetPublicSharedReportQueryHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes.
    /// </summary>
    public GetPublicSharedReportQueryHandlerTests()
    {
        _handler = new GetPublicSharedReportQueryHandler(_repository);
    }

    /// <summary>
    /// Valida que o relatório público é retornado com os dados de branding e flags de expiração corretas.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidShareToken_ShouldReturnSharedReportDto()
    {
        // Arrange
        var token = "a1b2c3d4e5f6789012345678";
        var renderModel = new ReportRenderModel
        {
            ReportTitle = "Relatório Executivo Mensal",
            WorkspaceName = "Loja Virtual Modelo",
            DateRangeStart = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            DateRangeEnd = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            Branding = new ReportBrandingSnapshot(
                "#2563EB", "#0F172A", null, null, null, "Agência Teste", "suporte@agencia.com", null, null),
            KpiSummary = new ReportKpiSummary { TotalSpend = 3000m, TotalRevenue = 12000m, BlendedRoas = 4.0m }
        };

        var report = GeneratedReport.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Relatório Executivo Mensal",
            renderModel.DateRangeStart,
            renderModel.DateRangeEnd,
            shareTokenExpiresAtUtc: DateTime.UtcNow.AddDays(15),
            pdfContent: null,
            reportDataPayloadJson: JsonSerializer.Serialize(renderModel),
            generatedAtUtc: DateTime.UtcNow,
            customToken: token).Value;

        _repository.GetReportByShareTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(report);

        var query = new GetPublicSharedReportQuery(token);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.ReportTitle.Should().Be("Relatório Executivo Mensal");
        dto.WorkspaceName.Should().Be("Loja Virtual Modelo");
        dto.IsExpired.Should().BeFalse();
        dto.Branding.AgencyName.Should().Be("Agência Teste");
        dto.KpiSummary.TotalSpend.Should().Be(3000m);
        dto.KpiSummary.BlendedRoas.Should().Be(4.0m);
    }

    /// <summary>
    /// Valida que token inexistente retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _repository.GetReportByShareTokenAsync("token-inexistente", Arg.Any<CancellationToken>())
            .Returns((GeneratedReport?)null);

        var query = new GetPublicSharedReportQuery("token-inexistente");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Report.NotFound");
    }
}
