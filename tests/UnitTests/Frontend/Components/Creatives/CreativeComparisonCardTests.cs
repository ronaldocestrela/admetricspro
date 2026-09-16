using Analytics.Application.Creatives.DTOs;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Creatives;
using Xunit;

namespace UnitTests.Frontend.Components.Creatives;

/// <summary>
/// Testes bUnit para o componente comparativo cross-platform <see cref="CreativeComparisonCard"/> (Subfase 5.2.2).
/// </summary>
public sealed class CreativeComparisonCardTests : BunitTestBase
{
    /// <summary>
    /// Valida que o card comparativo exibe o canal vencedor e as métricas de ambas as redes.
    /// </summary>
    [Fact]
    public void CreativeComparisonCard_ShouldRenderMetricsAndWinningPlatform()
    {
        // Arrange
        var meta = new CrossPlatformMetricsDto("MetaAds", 1, 1000m, 50000, 1500, 50m, 5000m, 3.0m, 0.67m, 20m, 5.0m);
        var tikTok = new CrossPlatformMetricsDto("TikTokAds", 1, 1000m, 60000, 1200, 25m, 2500m, 2.0m, 0.83m, 40m, 2.5m);

        var comparison = new CrossPlatformComparisonDto(
            "hash_teaser_1",
            "Vídeo Teaser Nova Linha",
            null,
            meta,
            tikTok,
            "MetaAds",
            -50m,
            50m,
            "MetaAds entregou melhor custo por conversão.");

        // Act
        var cut = Render<CreativeComparisonCard>(parameters => parameters
            .Add(p => p.Comparison, comparison));

        // Assert
        cut.Markup.Should().Contain("Vídeo Teaser Nova Linha");
        cut.Markup.Should().Contain("MetaAds");
        cut.Find("#badge-winning-platform").TextContent.Should().Contain("MetaAds");
        cut.Find("#meta-cpa").TextContent.Should().Contain("20");
        cut.Find("#tiktok-cpa").TextContent.Should().Contain("40");
        cut.Find("#comparison-efficiency-summary").TextContent.Should().Contain("MetaAds entregou melhor");
    }
}
