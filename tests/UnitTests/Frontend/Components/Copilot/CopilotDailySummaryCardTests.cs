using Analytics.Application.Copilot.DTOs;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Copilot;
using Xunit;

namespace UnitTests.Frontend.Components.Copilot;

/// <summary>
/// Testes bUnit para o componente <see cref="CopilotDailySummaryCard"/>.
/// </summary>
public sealed class CopilotDailySummaryCardTests : BunitTestBase
{
    /// <summary>
    /// Valida que o card de resumo diário renderiza a síntese executiva, vitórias, riscos e economia projetada.
    /// </summary>
    [Fact]
    public void CopilotDailySummaryCard_ShouldRenderExecutiveSummaryAndMetrics()
    {
        // Arrange
        var report = new DailyDiagnosticReportDto(
            WorkspaceId: Guid.NewGuid(),
            ReportDate: new DateTime(2026, 9, 16),
            ExecutiveSummary: "Foram identificadas 2 anomalias críticas demandando ação.",
            WinsSummary: "Campanhas de topo continuam com ROAS positivo.",
            RisksSummary: "Sobreposição de públicos no Meta Ads.",
            AudienceOverlapAnomalies: Array.Empty<AudienceOverlapAnomalyDto>(),
            SearchCannibalizationAnomalies: Array.Empty<SearchCannibalizationAnomalyDto>(),
            Actions: Array.Empty<CopilotRecommendationActionDto>(),
            EstimatedMonthlySavings: 1250.50m,
            CriticalAnomaliesCount: 2,
            HighAnomaliesCount: 1
        );

        // Act
        var cut = Render<CopilotDailySummaryCard>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Find("#copilot-executive-summary").TextContent.Should().Contain("Foram identificadas 2 anomalias críticas");
        cut.Find("#copilot-wins-section").TextContent.Should().Contain("Campanhas de topo");
        cut.Find("#copilot-risks-section").TextContent.Should().Contain("Sobreposição de públicos");
        cut.Find("#copilot-savings-val").TextContent.Should().MatchRegex(@"1[.,]250[.,]50");
        cut.Find("#copilot-critical-count").TextContent.Should().Be("2");
        cut.Find("#copilot-high-count").TextContent.Should().Be("1");
    }
}
