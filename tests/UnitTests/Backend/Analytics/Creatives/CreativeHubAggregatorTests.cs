using Analytics.Domain.Creatives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics.Creatives;

/// <summary>
/// Testes unitários para o agregador de ativos de mídia e comparador cross-platform <see cref="ICreativeHubAggregator"/> (Subfase 5.2.2).
/// </summary>
public sealed class CreativeHubAggregatorTests
{
    private readonly ICreativeHubAggregator _aggregator;

    /// <summary>
    /// Inicializa a suíte de testes instanciando o agregador.
    /// </summary>
    public CreativeHubAggregatorTests()
    {
        _aggregator = new CreativeHubAggregator();
    }

    /// <summary>
    /// Valida que o Meta Ads é eleito vencedor quando entrega menor CPA que o TikTok Ads para a mesma peça.
    /// </summary>
    [Fact]
    public void CompareCrossPlatform_ShouldIdentifyMetaAsWinner_WhenMetaHasLowerCpa()
    {
        // Arrange
        var assetFingerprint = "hash_video_promo_2026";
        var assetName = "Vídeo Teaser Nova Coleção";
        var previewUrl = "https://cdn.example.com/teaser.mp4";

        var date = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var metaMetrics = new List<CreativeDailyMetricPoint>
        {
            new(date, Impressions: 50000, Clicks: 1500, Spend: 1000m, Conversions: 50m, ConversionValue: 5000m, Frequency: 1.5m), // CPA: 20, ROAS: 5.0, CTR: 3.0%
        };

        var tikTokMetrics = new List<CreativeDailyMetricPoint>
        {
            new(date, Impressions: 60000, Clicks: 1200, Spend: 1000m, Conversions: 25m, ConversionValue: 2500m, Frequency: 1.4m), // CPA: 40, ROAS: 2.5, CTR: 2.0%
        };

        // Act
        var result = _aggregator.CompareCrossPlatform(assetFingerprint, assetName, previewUrl, metaMetrics, tikTokMetrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;

        comparison.AssetFingerprint.Should().Be(assetFingerprint);
        comparison.WinningPlatform.Should().Be("MetaAds");
        comparison.MetaMetrics.Cpa.Should().Be(20m);
        comparison.TikTokMetrics.Cpa.Should().Be(40m);
        comparison.MetaMetrics.Roas.Should().Be(5.0m);
        comparison.TikTokMetrics.Roas.Should().Be(2.5m);
        comparison.CpaDifferencePercentage.Should().Be(-50m); // Meta é 50% mais barato em CPA
        comparison.EfficiencySummary.Should().Contain("MetaAds");
    }

    /// <summary>
    /// Valida que o TikTok Ads é eleito vencedor quando entrega maior ROAS e menor CPA que o Meta Ads.
    /// </summary>
    [Fact]
    public void CompareCrossPlatform_ShouldIdentifyTikTokAsWinner_WhenTikTokHasHigherRoasAndLowerCpa()
    {
        // Arrange
        var assetFingerprint = "hash_ugc_review";
        var assetName = "UGC Depoimento Cliente";

        var date = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var metaMetrics = new List<CreativeDailyMetricPoint>
        {
            new(date, Impressions: 40000, Clicks: 800, Spend: 800m, Conversions: 16m, ConversionValue: 1600m, Frequency: 1.8m), // CPA: 50, ROAS: 2.0
        };

        var tikTokMetrics = new List<CreativeDailyMetricPoint>
        {
            new(date, Impressions: 80000, Clicks: 3200, Spend: 800m, Conversions: 40m, ConversionValue: 4000m, Frequency: 1.3m), // CPA: 20, ROAS: 5.0
        };

        // Act
        var result = _aggregator.CompareCrossPlatform(assetFingerprint, assetName, null, metaMetrics, tikTokMetrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;

        comparison.WinningPlatform.Should().Be("TikTokAds");
        comparison.MetaMetrics.Cpa.Should().Be(50m);
        comparison.TikTokMetrics.Cpa.Should().Be(20m);
        comparison.TikTokMetrics.Roas.Should().Be(5.0m);
        comparison.EfficiencySummary.Should().Contain("TikTokAds");
    }

    /// <summary>
    /// Valida que a comparação retorna falha de negócio quando ambas as redes não possuem registros.
    /// </summary>
    [Fact]
    public void CompareCrossPlatform_ShouldReturnFailure_WhenBothPlatformsHaveNoData()
    {
        // Arrange
        var metaMetrics = new List<CreativeDailyMetricPoint>();
        var tikTokMetrics = new List<CreativeDailyMetricPoint>();

        // Act
        var result = _aggregator.CompareCrossPlatform("hash_empty", "Criativo Vazio", null, metaMetrics, tikTokMetrics);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreativeComparison.NoData");
    }
}
