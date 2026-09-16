using Analytics.Domain.Reports;
using Analytics.Infrastructure.Reports;
using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários para o orquestrador de envio de relatórios <see cref="ReportDispatchService"/>.
/// </summary>
public sealed class ReportDispatchServiceTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IReportWhatsAppNotifier _whatsAppNotifier = Substitute.For<IReportWhatsAppNotifier>();
    private readonly IReportRepository _repository = Substitute.For<IReportRepository>();
    private readonly ReportDispatchService _dispatchService;

    /// <summary>
    /// Inicializa os substitutos e o serviço de despacho.
    /// </summary>
    public ReportDispatchServiceTests()
    {
        _dispatchService = new ReportDispatchService(_emailSender, _whatsAppNotifier, _repository);
    }

    /// <summary>
    /// Valida que quando o envio por e-mail e WhatsApp é bem-sucedido, registros de sucesso são persistidos.
    /// </summary>
    [Fact]
    public async Task DispatchReportAsync_WhenAllChannelsSucceed_ShouldRecordSuccessLogs()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        var recipients = new List<ReportRecipient>
        {
            ReportRecipient.Create("Diretoria Alfa", ReportDeliveryChannel.Email, "diretor@empresa.com").Value,
            ReportRecipient.Create("Gestor Alfa", ReportDeliveryChannel.WhatsApp, "+5511999998888").Value
        };

        var schedule = ReportSchedule.Create(
            scheduleId,
            workspaceId,
            "Relatório Semanal",
            ReportFrequency.Weekly,
            DayOfWeek.Monday,
            null,
            new TimeSpan(8, 0, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Both,
            ReportDeliveryChannel.Both,
            null, null, false, false, false, false,
            recipients,
            DateTime.UtcNow).Value;

        var report = GeneratedReport.Create(
            reportId,
            workspaceId,
            scheduleId,
            "Relatório Executivo",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            shareTokenExpiresAtUtc: DateTime.UtcNow.AddDays(30),
            pdfContent: new byte[] { 1, 2, 3 },
            reportDataPayloadJson: "{}",
            generatedAtUtc: DateTime.UtcNow).Value;

        var model = new ReportRenderModel
        {
            ReportTitle = "Relatório Executivo",
            WorkspaceName = "Cliente Teste",
            DateRangeStart = DateTime.UtcNow.AddDays(-7),
            DateRangeEnd = DateTime.UtcNow,
            Branding = ReportBrandingSnapshot.Default,
            KpiSummary = new ReportKpiSummary { TotalSpend = 1000m, BlendedRoas = 3.5m, TotalConversions = 50 },
            ShareUrl = "https://agency.com/r/abc"
        };

        _emailSender.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _whatsAppNotifier.SendReportMessageAsync(Arg.Any<ReportWhatsAppMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _dispatchService.DispatchReportAsync(report, schedule, model);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var logs = result.Value;
        logs.Should().HaveCount(2);
        logs.Should().OnlyContain(l => l.IsSuccess);
        logs.Should().Contain(l => l.Channel == ReportDeliveryChannel.Email && l.Recipient == "diretor@empresa.com");
        logs.Should().Contain(l => l.Channel == ReportDeliveryChannel.WhatsApp && l.Recipient == "+5511999998888");

        report.Status.Should().Be(ReportStatus.Dispatched);

        await _repository.Received(1).AddDispatchLogsAsync(
            Arg.Is<IEnumerable<ReportDispatchLog>>(list => list.Count() == 2),
            Arg.Any<CancellationToken>());

        await _repository.Received(1).UpdateGeneratedReportAsync(report, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que quando o envio por e-mail falha, o log de auditoria é registrado com falha e mensagem de erro técnica.
    /// </summary>
    [Fact]
    public async Task DispatchReportAsync_WhenEmailFails_ShouldRecordFailureLogForEmail()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        var recipients = new List<ReportRecipient>
        {
            ReportRecipient.Create("Diretoria Alfa", ReportDeliveryChannel.Email, "diretor@empresa.com").Value
        };

        var schedule = ReportSchedule.Create(
            scheduleId,
            workspaceId,
            "Relatório Diário",
            ReportFrequency.Daily,
            null, null,
            new TimeSpan(8, 0, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Pdf,
            ReportDeliveryChannel.Email,
            null, null, false, false, false, false,
            recipients,
            DateTime.UtcNow).Value;

        var report = GeneratedReport.Create(
            reportId,
            workspaceId,
            scheduleId,
            "Relatório Diário",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            null, null, "{}", DateTime.UtcNow).Value;

        var model = new ReportRenderModel
        {
            ReportTitle = "Relatório Diário",
            WorkspaceName = "Cliente Teste",
            Branding = ReportBrandingSnapshot.Default
        };

        _emailSender.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("Smtp.Timeout", "Tempo limite de conexão SMTP esgotado.")));

        // Act
        var result = await _dispatchService.DispatchReportAsync(report, schedule, model);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var logs = result.Value;
        logs.Should().HaveCount(1);
        logs[0].IsSuccess.Should().BeFalse();
        logs[0].ErrorMessage.Should().Contain("Tempo limite de conexão SMTP esgotado.");
    }
}
