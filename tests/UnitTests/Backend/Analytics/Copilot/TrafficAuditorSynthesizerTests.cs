using Analytics.Domain.Copilot;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics.Copilot;

/// <summary>
/// Testes unitários para o sintetizador de diagnóstico diário em linguagem natural (Subfase 5.3.2).
/// </summary>
public sealed class TrafficAuditorSynthesizerTests
{
    private readonly ITrafficAuditorSynthesizer _synthesizer;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TrafficAuditorSynthesizerTests"/>.
    /// </summary>
    public TrafficAuditorSynthesizerTests()
    {
        _synthesizer = new TrafficAuditorSynthesizer();
    }

    /// <summary>
    /// Valida que a síntese diária gera texto estruturado com seções de vitórias, riscos, anomalias e projeção de economia.
    /// </summary>
    [Fact]
    public void SynthesizeDailyDiagnostic_ShouldGenerateStructuredText_WithWinsRisksAndAnomalies()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var reportDate = new DateTime(2026, 9, 16);

        var action1 = new CopilotRecommendationAction(
            actionId: Guid.NewGuid(),
            actionType: CopilotActionType.PauseAdSet,
            targetEntityId: Guid.NewGuid(),
            targetEntityName: "Conjunto Sobreposto B",
            platform: "MetaAds",
            title: "Pausar conjunto redundante",
            description: "Pausa o conjunto de pior CPA para estancar concorrência interna."
        );

        var overlapAnomaly = new AudienceOverlapAnomaly(
            adSetIdA: Guid.NewGuid(),
            adSetNameA: "Conjunto A",
            campaignIdA: Guid.NewGuid(),
            campaignNameA: "Campanha Meta",
            adSetIdB: action1.TargetEntityId,
            adSetNameB: action1.TargetEntityName,
            campaignIdB: Guid.NewGuid(),
            campaignNameB: "Campanha Meta",
            overlapPercentage: 55.0m,
            cpmA: 25.00m,
            cpmB: 38.00m,
            severity: CopilotAnomalySeverity.Critical,
            description: "Sobreposição de 55% no Meta Ads.",
            suggestedAction: action1,
            sharedTargetingTags: new[] { "empreendedorismo", "startups" }
        );

        var action2 = new CopilotRecommendationAction(
            actionId: Guid.NewGuid(),
            actionType: CopilotActionType.AddNegativeKeyword,
            targetEntityId: Guid.NewGuid(),
            targetEntityName: "Campanha Search Google",
            platform: "GoogleAds",
            title: "Negativar palavra-chave em GoogleAds",
            description: "Negativa termo com CPA 3x mais caro que no Bing."
        );

        var cannibalizationAnomaly = new SearchCannibalizationAnomaly(
            searchTerm: "gestao de trafego",
            channelA: "GoogleAds",
            campaignIdA: action2.TargetEntityId,
            campaignNameA: action2.TargetEntityName,
            cpcA: 6.00m,
            cpaA: 150.00m,
            spendA: 1200m,
            channelB: "BingAds",
            campaignIdB: Guid.NewGuid(),
            campaignNameB: "Campanha Bing",
            cpcB: 2.00m,
            cpaB: 50.00m,
            spendB: 300m,
            disparityRatio: 3.0m,
            estimatedMonthlyWastedSpend: 800m,
            severity: CopilotAnomalySeverity.Critical,
            description: "Canibalização de termo entre Google e Bing.",
            suggestedAction: action2
        );

        // Act
        var report = _synthesizer.SynthesizeDailyDiagnostic(
            workspaceId,
            reportDate,
            new[] { overlapAnomaly },
            new[] { cannibalizationAnomaly });

        // Assert
        report.Should().NotBeNull();
        report.WorkspaceId.Should().Be(workspaceId);
        report.ReportDate.Should().Be(reportDate);
        report.ExecutiveSummary.Should().NotBeNullOrWhiteSpace();
        report.ExecutiveSummary.Should().Contain("anomalias críticas");
        report.RisksSummary.Should().Contain("Sobreposição");
        report.RisksSummary.Should().Contain("Canibalização");
        report.EstimatedMonthlySavings.Should().BeGreaterThan(0m);
        report.Actions.Should().HaveCount(2);
        report.CriticalAnomaliesCount.Should().Be(2);
    }

    /// <summary>
    /// Valida que a síntese diária emite um resumo positivo quando não houver anomalias operacionais.
    /// </summary>
    [Fact]
    public void SynthesizeDailyDiagnostic_ShouldReturnPositiveSummary_WhenNoAnomaliesDetected()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var reportDate = new DateTime(2026, 9, 16);

        // Act
        var report = _synthesizer.SynthesizeDailyDiagnostic(
            workspaceId,
            reportDate,
            Array.Empty<AudienceOverlapAnomaly>(),
            Array.Empty<SearchCannibalizationAnomaly>());

        // Assert
        report.Should().NotBeNull();
        report.CriticalAnomaliesCount.Should().Be(0);
        report.HighAnomaliesCount.Should().Be(0);
        report.Actions.Should().BeEmpty();
        report.ExecutiveSummary.Should().Contain("Campanhas operando dentro dos parâmetros de estabilidade");
    }
}
