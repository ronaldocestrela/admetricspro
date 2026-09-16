using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários para a entidade <see cref="GeneratedReport"/>.
/// </summary>
public sealed class GeneratedReportTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Valida que a criação gera token criptográfico seguro de 48 caracteres hex e inicializa os campos.
    /// </summary>
    [Fact]
    public void Create_ShouldGenerateUniqueSecureTokenAndInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 15, 23, 59, 59, DateTimeKind.Utc);
        var expiresAt = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);
        var now = DateTime.UtcNow;

        // Act
        var result = GeneratedReport.Create(
            id,
            _workspaceId,
            reportScheduleId: null,
            title: "Relatório de Performance Quinzenal",
            dateRangeStartUtc: start,
            dateRangeEndUtc: end,
            shareTokenExpiresAtUtc: expiresAt,
            pdfContent: null,
            reportDataPayloadJson: "{}",
            generatedAtUtc: now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;
        report.Id.Should().Be(id);
        report.WorkspaceId.Should().Be(_workspaceId);
        report.ReportScheduleId.Should().BeNull();
        report.Title.Should().Be("Relatório de Performance Quinzenal");
        report.ShareToken.Should().NotBeNullOrWhiteSpace();
        report.ShareToken.Length.Should().Be(48); // 24 bytes hex = 48 chars
        report.Status.Should().Be(ReportStatus.Generated);
        report.IsExpired(now).Should().BeFalse();
    }

    /// <summary>
    /// Valida o cálculo de expiração com base no tempo UTC informado.
    /// </summary>
    [Fact]
    public void IsExpired_WhenCurrentTimePastExpiration_ShouldReturnTrue()
    {
        // Arrange
        var expiresAt = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        var report = GeneratedReport.Create(
            Guid.NewGuid(),
            _workspaceId,
            null,
            "Relatório Expirado",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-15),
            shareTokenExpiresAtUtc: expiresAt,
            pdfContent: null,
            reportDataPayloadJson: "{}",
            generatedAtUtc: DateTime.UtcNow.AddDays(-20)).Value;

        // Act & Assert
        report.IsExpired(new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
        report.IsExpired(new DateTime(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    /// <summary>
    /// Valida que datas inconsistentes (final antes da inicial) geram erro de validação.
    /// </summary>
    [Fact]
    public void Create_WhenEndDateIsBeforeStartDate_ShouldFail()
    {
        // Act
        var result = GeneratedReport.Create(
            Guid.NewGuid(),
            _workspaceId,
            null,
            "Relatório Inválido",
            dateRangeStartUtc: new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            dateRangeEndUtc: new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), // Anterior
            shareTokenExpiresAtUtc: null,
            pdfContent: null,
            reportDataPayloadJson: "{}",
            generatedAtUtc: DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeneratedReport.InvalidDateRange");
    }

    /// <summary>
    /// Valida transições de status entre gerado, despachado e com falha.
    /// </summary>
    [Fact]
    public void MarkDispatchedAndFailed_ShouldUpdateStatus()
    {
        // Arrange
        var report = GeneratedReport.Create(
            Guid.NewGuid(),
            _workspaceId,
            null,
            "Relatório Status",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            shareTokenExpiresAtUtc: null,
            pdfContent: null,
            reportDataPayloadJson: "{}",
            generatedAtUtc: DateTime.UtcNow).Value;

        report.Status.Should().Be(ReportStatus.Generated);

        // Act & Assert
        report.MarkDispatched();
        report.Status.Should().Be(ReportStatus.Dispatched);

        report.MarkFailed();
        report.Status.Should().Be(ReportStatus.Failed);
    }
}
