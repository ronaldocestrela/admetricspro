using Analytics.Domain.Attribution;
using Analytics.Infrastructure.Attribution;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para o motor de atribuição multi-canal IAttributionCalculator,
/// cobrindo os modelos Primeiro Clique, Último Clique, Linear e conversões assistidas.
/// </summary>
public sealed class AttributionCalculatorTests
{
    private readonly AttributionCalculator _calculator = new();

    /// <summary>
    /// Valida que o modelo Primeiro Clique (First-Touch) atribui 100% da conversão e da receita ao primeiro canal da jornada.
    /// </summary>
    [Fact]
    public void CalculateModel_FirstTouch_AttributesEntireCreditToFirstChannel()
    {
        // Arrange
        var convDate = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var journeys = new List<ConversionJourney>
        {
            new("j1", "c1", convDate, 300m, new List<AttributionTouchpoint>
            {
                new("TikTok", "c_top", convDate.AddDays(-5), TouchpointType.Click),
                new("Meta", "c_mid", convDate.AddDays(-2), TouchpointType.Click),
                new("Google", "c_bottom", convDate.AddHours(-1), TouchpointType.Click)
            })
        };

        // Act
        var result = _calculator.CalculateModel(journeys, AttributionModelType.FirstTouch);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var channels = result.Value;
        channels.Should().HaveCount(3);

        var tiktok = channels.First(c => c.Channel == "TikTok");
        tiktok.AttributedConversions.Should().Be(1.0m);
        tiktok.AttributedRevenue.Should().Be(300m);
        tiktok.FirstTouchCount.Should().Be(1);
        tiktok.LastTouchCount.Should().Be(0);

        var meta = channels.First(c => c.Channel == "Meta");
        meta.AttributedConversions.Should().Be(0.0m);
        meta.AttributedRevenue.Should().Be(0.0m);

        var google = channels.First(c => c.Channel == "Google");
        google.AttributedConversions.Should().Be(0.0m);
        google.AttributedRevenue.Should().Be(0.0m);
    }

    /// <summary>
    /// Valida que o modelo Último Clique (Last-Touch) atribui 100% da conversão e da receita ao último canal da jornada.
    /// </summary>
    [Fact]
    public void CalculateModel_LastTouch_AttributesEntireCreditToLastChannel()
    {
        // Arrange
        var convDate = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var journeys = new List<ConversionJourney>
        {
            new("j1", "c1", convDate, 300m, new List<AttributionTouchpoint>
            {
                new("TikTok", "c_top", convDate.AddDays(-5), TouchpointType.Click),
                new("Meta", "c_mid", convDate.AddDays(-2), TouchpointType.Click),
                new("Google", "c_bottom", convDate.AddHours(-1), TouchpointType.Click)
            })
        };

        // Act
        var result = _calculator.CalculateModel(journeys, AttributionModelType.LastTouch);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var channels = result.Value;

        var google = channels.First(c => c.Channel == "Google");
        google.AttributedConversions.Should().Be(1.0m);
        google.AttributedRevenue.Should().Be(300m);
        google.LastTouchCount.Should().Be(1);

        var tiktok = channels.First(c => c.Channel == "TikTok");
        tiktok.AttributedConversions.Should().Be(0.0m);
        tiktok.AttributedRevenue.Should().Be(0.0m);

        var meta = channels.First(c => c.Channel == "Meta");
        meta.AttributedConversions.Should().Be(0.0m);
        meta.AttributedRevenue.Should().Be(0.0m);
    }

    /// <summary>
    /// Valida que o modelo Linear divide o crédito igualmente (1/N) entre todos os canais da jornada.
    /// </summary>
    [Fact]
    public void CalculateModel_Linear_DistributesCreditEquallyAcrossAllTouchpoints()
    {
        // Arrange
        var convDate = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var journeys = new List<ConversionJourney>
        {
            new("j1", "c1", convDate, 300m, new List<AttributionTouchpoint>
            {
                new("TikTok", "c_top", convDate.AddDays(-5), TouchpointType.Click),
                new("Meta", "c_mid", convDate.AddDays(-2), TouchpointType.Click),
                new("Google", "c_bottom", convDate.AddHours(-1), TouchpointType.Click)
            })
        };

        // Act
        var result = _calculator.CalculateModel(journeys, AttributionModelType.Linear);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var channels = result.Value;

        // 3 toques -> 1/3 para cada canal: 0.3333 conversões e R$ 100 cada
        foreach (var channel in channels)
        {
            channel.AttributedConversions.Should().BeApproximately(0.3333m, 0.001m);
            channel.AttributedRevenue.Should().Be(100.0m);
        }

        channels.Sum(c => c.AttributedRevenue).Should().Be(300.0m);
        channels.Sum(c => c.AttributedConversions).Should().BeApproximately(1.0m, 0.001m);
    }

    /// <summary>
    /// Valida o cálculo correto de conversões assistidas para canais intermediários.
    /// </summary>
    [Fact]
    public void CalculateModel_AssistedConversions_CountsIntermediateInteractions()
    {
        // Arrange
        var convDate = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var journeys = new List<ConversionJourney>
        {
            // Jornada 1: Meta (topo) -> TikTok (meio) -> Google (fundo)
            new("j1", "c1", convDate, 200m, new List<AttributionTouchpoint>
            {
                new("Meta", "m1", convDate.AddDays(-3), TouchpointType.Click),
                new("TikTok", "t1", convDate.AddDays(-1), TouchpointType.Click),
                new("Google", "g1", convDate.AddHours(-2), TouchpointType.Click)
            }),
            // Jornada 2: Bing (topo) -> Meta (meio) -> Google (fundo)
            new("j2", "c2", convDate, 400m, new List<AttributionTouchpoint>
            {
                new("Bing", "b1", convDate.AddDays(-4), TouchpointType.Click),
                new("Meta", "m2", convDate.AddDays(-2), TouchpointType.Click),
                new("Google", "g2", convDate.AddHours(-1), TouchpointType.Click)
            })
        };

        // Act
        var result = _calculator.CalculateModel(journeys, AttributionModelType.LastTouch);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var channels = result.Value;

        var meta = channels.First(c => c.Channel == "Meta");
        // Na jornada 1 Meta foi primeiro toque (assistiu). Na jornada 2 Meta foi meio (assistiu). Nenhuma foi último clique.
        meta.AssistedConversionsCount.Should().Be(2);

        var tiktok = channels.First(c => c.Channel == "TikTok");
        // Assistiu na jornada 1
        tiktok.AssistedConversionsCount.Should().Be(1);

        var google = channels.First(c => c.Channel == "Google");
        // Foi o último clique nas 2 jornadas, logo 0 conversões assistidas puras
        google.LastTouchCount.Should().Be(2);
        google.AssistedConversionsCount.Should().Be(0);
    }

    /// <summary>
    /// Valida o comparativo side-by-side gerado por CompareModels contendo os 3 modelos de atribuição e totais consolidados.
    /// </summary>
    [Fact]
    public void CompareModels_ReturnsSideBySideResultsForAllThreeModels()
    {
        // Arrange
        var convDate = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var journeys = new List<ConversionJourney>
        {
            new("j1", "c1", convDate, 500m, new List<AttributionTouchpoint>
            {
                new("Meta", "m1", convDate.AddDays(-2), TouchpointType.Click),
                new("Google", "g1", convDate.AddHours(-1), TouchpointType.Click)
            })
        };

        var channelCosts = new Dictionary<string, decimal>
        {
            ["Meta"] = 100m,
            ["Google"] = 100m
        };

        // Act
        var result = _calculator.CompareModels(journeys, channelCosts);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var comparison = result.Value;

        comparison.TotalJourneys.Should().Be(1);
        comparison.TotalConversions.Should().Be(1.0m);
        comparison.TotalConversionValue.Should().Be(500m);

        // First Touch: Meta = 500m, Google = 0m
        var firstMeta = comparison.FirstTouchChannels.First(c => c.Channel == "Meta");
        firstMeta.AttributedRevenue.Should().Be(500m);
        firstMeta.AttributedRoas.Should().Be(5.0m); // 500 / 100

        // Last Touch: Google = 500m, Meta = 0m
        var lastGoogle = comparison.LastTouchChannels.First(c => c.Channel == "Google");
        lastGoogle.AttributedRevenue.Should().Be(500m);
        lastGoogle.AttributedRoas.Should().Be(5.0m); // 500 / 100

        // Linear: Meta = 250m, Google = 250m
        var linearMeta = comparison.LinearChannels.First(c => c.Channel == "Meta");
        linearMeta.AttributedRevenue.Should().Be(250m);
        linearMeta.AttributedRoas.Should().Be(2.5m); // 250 / 100
    }

    /// <summary>
    /// Valida que jornada com um único touchpoint gera créditos idênticos em todos os modelos.
    /// </summary>
    [Fact]
    public void CalculateModel_WhenSingleTouchpoint_AllModelsProduceSameCredit()
    {
        // Arrange
        var convDate = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var journeys = new List<ConversionJourney>
        {
            new("j1", "c1", convDate, 150m, new List<AttributionTouchpoint>
            {
                new("Google", "brand_search", convDate.AddHours(-1), TouchpointType.Click)
            })
        };

        // Act
        var firstResult = _calculator.CalculateModel(journeys, AttributionModelType.FirstTouch);
        var lastResult = _calculator.CalculateModel(journeys, AttributionModelType.LastTouch);
        var linearResult = _calculator.CalculateModel(journeys, AttributionModelType.Linear);

        // Assert
        firstResult.Value.First().AttributedRevenue.Should().Be(150m);
        lastResult.Value.First().AttributedRevenue.Should().Be(150m);
        linearResult.Value.First().AttributedRevenue.Should().Be(150m);
    }

    /// <summary>
    /// Valida que touchpoints ocorridos após a data de conversão são descartados da análise.
    /// </summary>
    [Fact]
    public void CalculateModel_DiscardsTouchpointsAfterConversionDate()
    {
        // Arrange
        var convDate = new DateTime(2026, 9, 15, 18, 0, 0, DateTimeKind.Utc);
        var journeys = new List<ConversionJourney>
        {
            new("j1", "c1", convDate, 200m, new List<AttributionTouchpoint>
            {
                new("Meta", "m1", convDate.AddHours(-2), TouchpointType.Click),
                // Toque posterior à conversão (ex: evento tardio)
                new("TikTok", "t1", convDate.AddHours(2), TouchpointType.Click)
            })
        };

        // Act
        var result = _calculator.CalculateModel(journeys, AttributionModelType.LastTouch);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var channels = result.Value;
        var meta = channels.First(c => c.Channel == "Meta");
        meta.AttributedConversions.Should().Be(1.0m);
        meta.AttributedRevenue.Should().Be(200m);

        var tiktok = channels.FirstOrDefault(c => c.Channel == "TikTok");
        (tiktok == null || tiktok.AttributedConversions == 0).Should().BeTrue();
    }

    /// <summary>
    /// Valida falha quando o valor de conversão é negativo.
    /// </summary>
    [Fact]
    public void CalculateModel_WhenNegativeConversionValue_ReturnsFailureResult()
    {
        // Arrange
        var journeys = new List<ConversionJourney>
        {
            new("j1", "c1", DateTime.UtcNow, -50m, new List<AttributionTouchpoint>
            {
                new("Meta", "m1", DateTime.UtcNow.AddMinutes(-5), TouchpointType.Click)
            })
        };

        // Act
        var result = _calculator.CalculateModel(journeys, AttributionModelType.LastTouch);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Attribution.InvalidConversionValue");
    }
}
