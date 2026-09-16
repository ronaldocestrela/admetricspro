using Analytics.Domain.Copilot;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics.Copilot;

/// <summary>
/// Testes unitários para o detector de anomalias de sobreposição de públicos no Meta Ads (Subfase 5.3.1).
/// </summary>
public sealed class MetaAudienceOverlapDetectorTests
{
    private readonly IAudienceOverlapDetector _detector;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MetaAudienceOverlapDetectorTests"/>.
    /// </summary>
    public MetaAudienceOverlapDetectorTests()
    {
        _detector = new MetaAudienceOverlapDetector();
    }

    /// <summary>
    /// Valida que uma anomalia com severidade crítica é gerada quando a sobreposição for igual ou superior a 50%.
    /// </summary>
    [Fact]
    public void DetectOverlaps_ShouldReturnCriticalAnomaly_WhenTargetingOverlapIsAtOrAboveFiftyPercent()
    {
        // Arrange: Dois AdSets ativos disputando os mesmos públicos-alvo com 60% de sobreposição
        var adSet1 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto 1 - Empreendedorismo e Negócios",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha Conversão Vendas",
            status: "Active",
            spend: 1500m,
            impressions: 45000,
            cpm: 33.33m,
            cpa: 75.00m,
            targetingTags: new[] { "empreendedorismo", "startups", "marketing digital", "vendas online", "pequenas empresas" }
        );

        var adSet2 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto 2 - Gestão e Negócios Digitais",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha Conversão Vendas",
            status: "Active",
            spend: 1200m,
            impressions: 30000,
            cpm: 40.00m,
            cpa: 110.00m, // Pior CPA
            targetingTags: new[] { "empreendedorismo", "startups", "marketing digital", "gestão financeira" }
        );

        // Act
        var anomalies = _detector.DetectOverlaps(new[] { adSet1, adSet2 });

        // Assert
        anomalies.Should().NotBeNull();
        anomalies.Should().HaveCount(1);

        var anomaly = anomalies[0];
        anomaly.OverlapPercentage.Should().Be(50.0m);
        anomaly.Severity.Should().Be(CopilotAnomalySeverity.Critical);
        anomaly.SharedTargetingTags.Should().Contain(new[] { "empreendedorismo", "startups", "marketing digital" });
        anomaly.Description.Should().Contain("Sobreposição crítica de público");

        anomaly.SuggestedAction.Should().NotBeNull();
        anomaly.SuggestedAction.TargetEntityId.Should().Be(adSet2.AdSetId);
        anomaly.SuggestedAction.ActionType.Should().Be(CopilotActionType.PauseAdSet);
        anomaly.SuggestedAction.Title.Should().Contain("Pausar conjunto redundante");
    }

    /// <summary>
    /// Valida que uma anomalia com severidade High é gerada quando a sobreposição estiver entre 30% e 50%.
    /// </summary>
    [Fact]
    public void DetectOverlaps_ShouldReturnHighSeverityAnomaly_WhenOverlapIsBetweenThirtyAndFiftyPercent()
    {
        // Arrange: Dois AdSets com sobreposição moderada (aprox. 33.3%)
        var adSet1 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto A",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha Topo",
            status: "Active",
            spend: 800m,
            impressions: 20000,
            cpm: 40.00m,
            cpa: 50.00m,
            targetingTags: new[] { "marketing", "publicidade", "branding", "seo" }
        );

        var adSet2 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto B",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha Topo",
            status: "Active",
            spend: 950m,
            impressions: 21000,
            cpm: 45.23m,
            cpa: 85.00m,
            targetingTags: new[] { "branding", "seo", "redes sociais", "design" }
        );

        // Act
        var anomalies = _detector.DetectOverlaps(new[] { adSet1, adSet2 });

        // Assert
        anomalies.Should().HaveCount(1);
        var anomaly = anomalies[0];
        anomaly.OverlapPercentage.Should().BeApproximately(33.33m, 0.1m);
        anomaly.Severity.Should().Be(CopilotAnomalySeverity.High);
    }

    /// <summary>
    /// Valida que nenhuma anomalia é gerada quando a taxa de sobreposição estiver abaixo de 30%.
    /// </summary>
    [Fact]
    public void DetectOverlaps_ShouldReturnEmpty_WhenOverlapIsBelowThirtyPercent()
    {
        // Arrange: Públicos com baixa sobreposição (< 30%)
        var adSet1 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto A",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha 1",
            status: "Active",
            spend: 500m,
            impressions: 10000,
            cpm: 25.00m,
            cpa: 40.00m,
            targetingTags: new[] { "tecnologia", "gadgets", "inovacao", "software", "nuvem" }
        );

        var adSet2 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto B",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha 2",
            status: "Active",
            spend: 600m,
            impressions: 12000,
            cpm: 28.00m,
            cpa: 42.00m,
            targetingTags: new[] { "moda", "estilo", "beleza", "vestuario", "tecnologia" }
        );

        // Act
        var anomalies = _detector.DetectOverlaps(new[] { adSet1, adSet2 });

        // Assert
        anomalies.Should().BeEmpty();
    }

    /// <summary>
    /// Valida que conjuntos pausados ou inativos são desconsiderados na análise de overlap.
    /// </summary>
    [Fact]
    public void DetectOverlaps_ShouldIgnorePausedOrInactiveAdSets()
    {
        // Arrange: Um dos conjuntos está pausado
        var adSet1 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto Ativo",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha 1",
            status: "Active",
            spend: 1000m,
            impressions: 20000,
            cpm: 30.00m,
            cpa: 50.00m,
            targetingTags: new[] { "empreendedorismo", "startups" }
        );

        var adSet2 = new AdSetAudienceTargeting(
            adSetId: Guid.NewGuid(),
            adSetName: "Conjunto Pausado",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha 1",
            status: "Paused",
            spend: 0m,
            impressions: 0,
            cpm: 0m,
            cpa: 0m,
            targetingTags: new[] { "empreendedorismo", "startups" }
        );

        // Act
        var anomalies = _detector.DetectOverlaps(new[] { adSet1, adSet2 });

        // Assert
        anomalies.Should().BeEmpty();
    }
}
