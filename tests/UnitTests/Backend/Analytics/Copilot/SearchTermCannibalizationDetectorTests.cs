using Analytics.Domain.Copilot;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics.Copilot;

/// <summary>
/// Testes unitários para o detector de canibalização e disputa de termos de busca (Subfase 5.3.1).
/// Avalia conflitos entre Google Ads e Bing Ads, bem como auto-concorrência interna.
/// </summary>
public sealed class SearchTermCannibalizationDetectorTests
{
    private readonly ISearchTermCannibalizationDetector _detector;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SearchTermCannibalizationDetectorTests"/>.
    /// </summary>
    public SearchTermCannibalizationDetectorTests()
    {
        _detector = new SearchTermCannibalizationDetector();
    }

    /// <summary>
    /// Valida detecção de anomalia de canibalização com severidade crítica quando o mesmo termo disputa Google e Bing com alta disparidade de CPA.
    /// </summary>
    [Fact]
    public void DetectCannibalization_ShouldReturnAnomaly_WhenSameKeywordCompetesOnGoogleAndBingWithHighCpaDisparity()
    {
        // Arrange: Termo disputado simultaneamente no Google e no Bing com grande disparidade de CPA
        var kwGoogle = new SearchKeywordPerformance(
            platform: "GoogleAds",
            campaignId: Guid.NewGuid(),
            campaignName: "Search - Leads Alta Intenção",
            adGroupId: Guid.NewGuid(),
            adGroupName: "Termos Gerais de Gestão",
            keyword: "gestao de trafego pago",
            matchType: "Phrase",
            spend: 1500m,
            clicks: 250,
            conversions: 10m,
            cpc: 6.00m,
            cpa: 150.00m
        );

        var kwBing = new SearchKeywordPerformance(
            platform: "BingAds",
            campaignId: Guid.NewGuid(),
            campaignName: "Microsoft Search - Conversão",
            adGroupId: Guid.NewGuid(),
            adGroupName: "Gestão Tráfego",
            keyword: "gestao de trafego pago",
            matchType: "Exact",
            spend: 400m,
            clicks: 160,
            conversions: 8m,
            cpc: 2.50m,
            cpa: 50.00m
        );

        // Act
        var anomalies = _detector.DetectCannibalization(new[] { kwGoogle, kwBing });

        // Assert
        anomalies.Should().NotBeNull();
        anomalies.Should().HaveCount(1);

        var anomaly = anomalies[0];
        anomaly.SearchTerm.Should().Be("gestao de trafego pago");
        anomaly.DisparityRatio.Should().Be(3.0m);
        anomaly.Severity.Should().Be(CopilotAnomalySeverity.Critical);
        anomaly.EstimatedMonthlyWastedSpend.Should().Be(1000m);
        anomaly.Description.Should().Contain("Canibalização severa detectada");

        anomaly.SuggestedAction.Should().NotBeNull();
        anomaly.SuggestedAction.ActionType.Should().Be(CopilotActionType.AddNegativeKeyword);
        anomaly.SuggestedAction.Platform.Should().Be("GoogleAds");
        anomaly.SuggestedAction.Title.Should().Contain("Negativar palavra-chave em GoogleAds");
    }

    /// <summary>
    /// Valida que termos de busca com acentos e variações de caixa alta são normalizados e agrupados corretamente.
    /// </summary>
    [Fact]
    public void DetectCannibalization_ShouldNormalizeAccentsAndCasing()
    {
        // Arrange: Termo com acento e caixa alta vs minúsculo sem acento
        var kw1 = new SearchKeywordPerformance(
            platform: "GoogleAds",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha A",
            adGroupId: Guid.NewGuid(),
            adGroupName: "Grupo 1",
            keyword: "Gestão de Tráfego Pago",
            matchType: "Phrase",
            spend: 800m,
            clicks: 100,
            conversions: 4m,
            cpc: 8.00m,
            cpa: 200.00m
        );

        var kw2 = new SearchKeywordPerformance(
            platform: "BingAds",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha B",
            adGroupId: Guid.NewGuid(),
            adGroupName: "Grupo 2",
            keyword: "gestao de trafego pago",
            matchType: "Phrase",
            spend: 300m,
            clicks: 60,
            conversions: 5m,
            cpc: 5.00m,
            cpa: 60.00m
        );

        // Act
        var anomalies = _detector.DetectCannibalization(new[] { kw1, kw2 });

        // Assert: Ambos devem ser unificados sob a forma normalizada
        anomalies.Should().HaveCount(1);
        anomalies[0].SearchTerm.Should().Be("gestao de trafego pago");
    }

    /// <summary>
    /// Valida que nenhuma anomalia é gerada quando a performance entre os canais for equilibrada.
    /// </summary>
    [Fact]
    public void DetectCannibalization_ShouldReturnEmpty_WhenPerformanceIsBalanced()
    {
        // Arrange: Palavras em canais diferentes porém com CPA e CPC equilibrados (< 1.5x disparidade)
        var kw1 = new SearchKeywordPerformance(
            platform: "GoogleAds",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha Google",
            adGroupId: Guid.NewGuid(),
            adGroupName: "Grupo 1",
            keyword: "software crm vendas",
            matchType: "Exact",
            spend: 500m,
            clicks: 100,
            conversions: 10m,
            cpc: 5.00m,
            cpa: 50.00m
        );

        var kw2 = new SearchKeywordPerformance(
            platform: "BingAds",
            campaignId: Guid.NewGuid(),
            campaignName: "Campanha Bing",
            adGroupId: Guid.NewGuid(),
            adGroupName: "Grupo 2",
            keyword: "software crm vendas",
            matchType: "Exact",
            spend: 440m,
            clicks: 100,
            conversions: 8m,
            cpc: 4.40m,
            cpa: 55.00m
        );

        // Act
        var anomalies = _detector.DetectCannibalization(new[] { kw1, kw2 });

        // Assert: Nenhuma anomalia gerada
        anomalies.Should().BeEmpty();
    }
}
