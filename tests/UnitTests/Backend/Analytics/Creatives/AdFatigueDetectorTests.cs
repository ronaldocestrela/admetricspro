using Analytics.Domain.Creatives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics.Creatives;

/// <summary>
/// Testes unitários para o motor de cálculo de fadiga de criativos <see cref="IAdFatigueDetector"/> (Subfase 5.2.1).
/// </summary>
public sealed class AdFatigueDetectorTests
{
    private readonly IAdFatigueDetector _detector;

    /// <summary>
    /// Inicializa a suíte de testes instanciando o detector.
    /// </summary>
    public AdFatigueDetectorTests()
    {
        _detector = new AdFatigueDetector();
    }

    /// <summary>
    /// Valida que a fadiga de criativo é diagnosticada quando há queda contínua de CTR e frequência de exibição elevada.
    /// </summary>
    [Fact]
    public void AnalyzeFatigue_ShouldDetectFatigue_WhenCtrDropsProgressivelyAndFrequencyIsHigh()
    {
        // Arrange: 7 dias com CTR caindo de 3.2% para 1.4% e frequência acumulada de 3.2
        var adId = Guid.NewGuid();
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var metrics = new List<CreativeDailyMetricPoint>
        {
            new(startDate.AddDays(0), Impressions: 10000, Clicks: 320, Spend: 100m, Conversions: 10m, ConversionValue: 300m, Frequency: 1.2m), // CTR 3.2%
            new(startDate.AddDays(1), Impressions: 11000, Clicks: 330, Spend: 110m, Conversions: 11m, ConversionValue: 330m, Frequency: 1.6m), // CTR 3.0%
            new(startDate.AddDays(2), Impressions: 12000, Clicks: 312, Spend: 120m, Conversions: 9m, ConversionValue: 270m, Frequency: 2.0m),  // CTR 2.6%
            new(startDate.AddDays(3), Impressions: 13000, Clicks: 299, Spend: 130m, Conversions: 8m, ConversionValue: 240m, Frequency: 2.4m),  // CTR 2.3%
            new(startDate.AddDays(4), Impressions: 14000, Clicks: 280, Spend: 140m, Conversions: 7m, ConversionValue: 210m, Frequency: 2.7m),  // CTR 2.0%
            new(startDate.AddDays(5), Impressions: 15000, Clicks: 255, Spend: 150m, Conversions: 6m, ConversionValue: 180m, Frequency: 3.0m),  // CTR 1.7%
            new(startDate.AddDays(6), Impressions: 16000, Clicks: 224, Spend: 160m, Conversions: 5m, ConversionValue: 150m, Frequency: 3.4m)   // CTR 1.4%
        };

        // Act
        var result = _detector.AnalyzeFatigue(adId, "Vídeo Promoção Oferta", "MetaAds", "https://cdn.example.com/ad1.mp4", metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var fatigue = result.Value;

        fatigue.AdId.Should().Be(adId);
        fatigue.Status.Should().Be(CreativeFatigueStatus.Fatigued);
        fatigue.ReplacementSuggested.Should().BeTrue();
        fatigue.CtrChangePercentage.Should().BeLessThan(-40m);
        fatigue.CtrTrendSlope.Should().BeLessThan(0);
        fatigue.AverageFrequency.Should().BeGreaterThan(2.0m);
        fatigue.CurrentFrequency.Should().Be(3.4m);
        fatigue.Reason.Should().Contain("queda");
        fatigue.ActionRecommendation.Should().Contain("Substitua");
    }

    /// <summary>
    /// Valida que o criativo é classificado como saudável quando o CTR permanece estável e a frequência baixa.
    /// </summary>
    [Fact]
    public void AnalyzeFatigue_ShouldClassifyHealthy_WhenCtrIsStableAndFrequencyIsLow()
    {
        // Arrange: 7 dias com CTR estável em ~2.5% e frequência baixa (1.3)
        var adId = Guid.NewGuid();
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var metrics = new List<CreativeDailyMetricPoint>
        {
            new(startDate.AddDays(0), Impressions: 10000, Clicks: 250, Spend: 100m, Conversions: 8m, ConversionValue: 250m, Frequency: 1.1m), // CTR 2.5%
            new(startDate.AddDays(1), Impressions: 10000, Clicks: 245, Spend: 100m, Conversions: 8m, ConversionValue: 250m, Frequency: 1.2m), // CTR 2.45%
            new(startDate.AddDays(2), Impressions: 10000, Clicks: 260, Spend: 100m, Conversions: 9m, ConversionValue: 280m, Frequency: 1.2m), // CTR 2.6%
            new(startDate.AddDays(3), Impressions: 10000, Clicks: 255, Spend: 100m, Conversions: 8m, ConversionValue: 260m, Frequency: 1.3m), // CTR 2.55%
            new(startDate.AddDays(4), Impressions: 10000, Clicks: 250, Spend: 100m, Conversions: 8m, ConversionValue: 250m, Frequency: 1.3m), // CTR 2.5%
            new(startDate.AddDays(5), Impressions: 10000, Clicks: 252, Spend: 100m, Conversions: 8m, ConversionValue: 250m, Frequency: 1.4m), // CTR 2.52%
            new(startDate.AddDays(6), Impressions: 10000, Clicks: 258, Spend: 100m, Conversions: 9m, ConversionValue: 270m, Frequency: 1.4m)  // CTR 2.58%
        };

        // Act
        var result = _detector.AnalyzeFatigue(adId, "Carrossel Novo Produto", "TikTokAds", null, metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var fatigue = result.Value;

        fatigue.Status.Should().Be(CreativeFatigueStatus.Healthy);
        fatigue.ReplacementSuggested.Should().BeFalse();
        fatigue.CtrChangePercentage.Should().BeGreaterThan(-10m);
        fatigue.AverageFrequency.Should().BeLessThan(2.0m);
    }

    /// <summary>
    /// Valida que o criativo recebe classificação de advertência (Warning) com queda leve ou frequência moderada.
    /// </summary>
    [Fact]
    public void AnalyzeFatigue_ShouldClassifyWarning_WhenCtrDropsModeratelyOrFrequencyApproachesThreshold()
    {
        // Arrange: CTR cai de 2.5% para 2.15% (~ -14%) com frequência moderada de 2.2
        var adId = Guid.NewGuid();
        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var metrics = new List<CreativeDailyMetricPoint>
        {
            new(startDate.AddDays(0), Impressions: 10000, Clicks: 250, Spend: 100m, Conversions: 8m, ConversionValue: 250m, Frequency: 1.5m), // 2.5%
            new(startDate.AddDays(1), Impressions: 10000, Clicks: 240, Spend: 100m, Conversions: 8m, ConversionValue: 240m, Frequency: 1.7m), // 2.4%
            new(startDate.AddDays(2), Impressions: 10000, Clicks: 235, Spend: 100m, Conversions: 7m, ConversionValue: 230m, Frequency: 1.9m), // 2.35%
            new(startDate.AddDays(3), Impressions: 10000, Clicks: 230, Spend: 100m, Conversions: 7m, ConversionValue: 220m, Frequency: 2.0m), // 2.3%
            new(startDate.AddDays(4), Impressions: 10000, Clicks: 225, Spend: 100m, Conversions: 7m, ConversionValue: 220m, Frequency: 2.1m), // 2.25%
            new(startDate.AddDays(5), Impressions: 10000, Clicks: 220, Spend: 100m, Conversions: 6m, ConversionValue: 200m, Frequency: 2.2m), // 2.2%
            new(startDate.AddDays(6), Impressions: 10000, Clicks: 215, Spend: 100m, Conversions: 6m, ConversionValue: 200m, Frequency: 2.3m)  // 2.15%
        };

        // Act
        var result = _detector.AnalyzeFatigue(adId, "Banner Promo 20%", "MetaAds", null, metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CreativeFatigueStatus.Warning);
        result.Value.ReplacementSuggested.Should().BeFalse();
    }

    /// <summary>
    /// Valida que a análise rejeita dados com menos de 3 dias de histórico.
    /// </summary>
    [Fact]
    public void AnalyzeFatigue_ShouldReturnFailure_WhenMetricsHaveLessThanThreeDays()
    {
        // Arrange
        var adId = Guid.NewGuid();
        var metrics = new List<CreativeDailyMetricPoint>
        {
            new(DateTime.UtcNow.AddDays(-1), 1000, 20, 50m, 2m, 80m, 1.1m),
            new(DateTime.UtcNow, 1200, 22, 55m, 2m, 85m, 1.2m)
        };

        // Act
        var result = _detector.AnalyzeFatigue(adId, "Criativo Novo", "MetaAds", null, metrics);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreativeFatigue.InsufficientData");
    }

    /// <summary>
    /// Valida que a análise rejeita métricas com zero impressões acumuladas.
    /// </summary>
    [Fact]
    public void AnalyzeFatigue_ShouldReturnFailure_WhenTotalImpressionsAreZero()
    {
        // Arrange
        var adId = Guid.NewGuid();
        var metrics = new List<CreativeDailyMetricPoint>
        {
            new(DateTime.UtcNow.AddDays(-3), 0, 0, 0m, 0m, 0m, 0m),
            new(DateTime.UtcNow.AddDays(-2), 0, 0, 0m, 0m, 0m, 0m),
            new(DateTime.UtcNow.AddDays(-1), 0, 0, 0m, 0m, 0m, 0m)
        };

        // Act
        var result = _detector.AnalyzeFatigue(adId, "Criativo Zerado", "MetaAds", null, metrics);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreativeFatigue.NoImpressions");
    }
}
