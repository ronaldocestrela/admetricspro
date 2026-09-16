using Analytics.Application.Reports.DTOs;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages.Reports;
using WebApp.Services.Reports;
using Xunit;

namespace UnitTests.Frontend.Components.Reports;

/// <summary>
/// Testes de componente bUnit para a página de gestão de relatórios automatizados <see cref="ReportsManagementPage"/> (Subfase 5.4.3).
/// </summary>
public sealed class ReportsManagementPageTests : BunitTestBase
{
    private readonly IReportClientService _reportClientService = Substitute.For<IReportClientService>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte registrando o serviço de relatórios nos serviços do bUnit.
    /// </summary>
    public ReportsManagementPageTests()
    {
        Services.AddSingleton(_reportClientService);

        var workspaceList = new List<WorkspaceDto>
        {
            new(_workspaceId, "Workspace Cliente Alpha", "12345678000199", 50000m, "E-commerce", true, DateTime.UtcNow, null)
        };

        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<WorkspaceDto>>.Success(workspaceList));
    }

    /// <summary>
    /// Valida que a página carrega os agendamentos e renderiza a tabela de agendamentos com os botões de ação.
    /// </summary>
    [Fact]
    public void ReportsManagementPage_ShouldRenderSchedules_WhenDataIsLoaded()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var recipient = new ReportRecipientDto("Diretoria", ReportDeliveryChannel.Email, "diretoria@clientealpha.com");
        var scheduleDto = new ReportScheduleDto(
            scheduleId,
            _workspaceId,
            "Relatório Semanal Executivo",
            ReportFrequency.Weekly,
            DayOfWeek.Monday,
            null,
            new TimeSpan(8, 0, 0),
            ReportDateRangeType.Last7Days,
            ReportOutputFormat.Both,
            ReportDeliveryChannel.Both,
            "Resumo Semanal",
            "Notas de acompanhamento",
            IncludeCopilotInsights: true,
            IncludeTopCreatives: true,
            IncludeChannelBreakdown: true,
            IncludePacingSummary: true,
            IsActive: true,
            LastExecutedAtUtc: DateTime.UtcNow.AddDays(-1),
            NextExecutionAtUtc: DateTime.UtcNow.AddDays(6),
            CreatedAtUtc: DateTime.UtcNow.AddDays(-10),
            Recipients: new List<ReportRecipientDto> { recipient });

        _reportClientService.GetSchedulesAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ReportScheduleDto>>.Success(new List<ReportScheduleDto> { scheduleDto }));

        _reportClientService.GetHistoryAsync(_workspaceId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<GeneratedReportSummaryDto>>.Success(new List<GeneratedReportSummaryDto>()));

        // Act
        var cut = Render<ReportsManagementPage>(parameters => parameters
            .Add(p => p.WorkspaceId, _workspaceId));

        // Assert
        cut.WaitForState(() => cut.FindAll("#table-report-schedules").Count > 0);

        cut.Markup.Should().Contain("Relatórios Executivos White-Label");
        cut.Markup.Should().Contain("Relatório Semanal Executivo");
        cut.Markup.Should().Contain("contatos");
        cut.Find("#btn-new-schedule").Should().NotBeNull();
        cut.Find("#btn-generate-report").Should().NotBeNull();
        cut.Find($"#btn-trigger-schedule-{scheduleId}").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que a alternância para a aba de histórico renderiza a listagem de relatórios emitidos.
    /// </summary>
    [Fact]
    public void ReportsManagementPage_ShouldRenderReportsHistory_WhenTabClicked()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reportDto = new GeneratedReportSummaryDto(
            reportId,
            _workspaceId,
            null,
            "Relatório Mensal - Agosto 2026",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            "token-seguro-123456",
            DateTime.UtcNow.AddDays(30),
            ReportStatus.Generated,
            DateTime.UtcNow.AddDays(-1),
            "/r/token-seguro-123456",
            HasPdf: true);

        _reportClientService.GetSchedulesAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ReportScheduleDto>>.Success(new List<ReportScheduleDto>()));

        _reportClientService.GetHistoryAsync(_workspaceId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<GeneratedReportSummaryDto>>.Success(new List<GeneratedReportSummaryDto> { reportDto }));

        // Act
        var cut = Render<ReportsManagementPage>(parameters => parameters
            .Add(p => p.WorkspaceId, _workspaceId));

        cut.WaitForState(() => cut.FindAll("#btn-tab-history").Count > 0);
        cut.Find("#btn-tab-history").Click();

        // Assert
        cut.WaitForState(() => cut.FindAll("#table-generated-reports").Count > 0);
        cut.Markup.Should().Contain("Relatório Mensal - Agosto 2026");
        cut.Markup.Should().Contain("Gerado");
        cut.Find($"#btn-view-report-{reportId}").Should().NotBeNull();
        cut.Find($"#btn-copy-token-{reportId}").Should().NotBeNull();
    }
}
