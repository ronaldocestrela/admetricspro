using Analytics.Application.Dashboard.DTOs;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Dashboard;
using Xunit;

namespace UnitTests.Frontend.Components.Dashboard;

/// <summary>
/// Testes unitários com bUnit para os cartões de métricas principais (<see cref="MetricCardsGrid"/> e <see cref="MetricCard"/>).
/// Valida renderização dos 6 cartões (Spend, CPC, CPM, CTR, CPA, ROAS), comparação com período anterior,
/// polaridade semântica de custos (CPC e CPA invertidos) e estado de loading (skeleton).
/// </summary>
public sealed class MetricCardsGridTests : BunitTestBase
{
    private static List<ExecutiveMetricItemDto> CreateSampleMetrics() => new()
    {
        new("Spend", "Investimento Total", 1000m, 800m, 25.0m, true, "Currency"),
        new("Cpc", "Custo por Clique (CPC)", 1.50m, 2.00m, -25.0m, true, "Currency"),
        new("Cpm", "Custo por Mil Impressões (CPM)", 15.0m, 18.0m, -16.67m, true, "Currency"),
        new("Ctr", "Taxa de Cliques (CTR)", 3.50m, 2.80m, 25.0m, true, "Percentage"),
        new("Cpa", "Custo por Aquisição (CPA)", 20.0m, 25.0m, -20.0m, true, "Currency"),
        new("Roas", "Retorno sobre Ad Spend (ROAS)", 4.00m, 3.20m, 25.0m, true, "Multiplier")
    };

    /// <summary>
    /// Valida que a grade renderiza os 6 cartões mandatários de métricas com seus rótulos textuais.
    /// </summary>
    [Fact]
    public void MetricCardsGrid_WhenRendered_DisplaysAllSixMandatoryMetrics()
    {
        // Arrange
        var metrics = CreateSampleMetrics();

        // Act
        var cut = Render<MetricCardsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics)
            .Add(p => p.Currency, "BRL")
            .Add(p => p.IsLoading, false));

        // Assert
        var cards = cut.FindAll(".metric-card");
        cards.Should().HaveCount(6);

        var markup = cut.Markup;
        markup.Should().Contain("Investimento Total");
        markup.Should().Contain("Custo por Clique (CPC)");
        markup.Should().Contain("Custo por Mil Impressões (CPM)");
        markup.Should().Contain("Taxa de Cliques (CTR)");
        markup.Should().Contain("Custo por Aquisição (CPA)");
        markup.Should().Contain("Retorno sobre Ad Spend (ROAS)");
    }

    /// <summary>
    /// Valida que os valores atuais e do período anterior são formatados e renderizados corretamente.
    /// </summary>
    [Fact]
    public void MetricCardsGrid_DisplaysCurrentAndPreviousValuesCorrectly()
    {
        // Arrange
        var metrics = CreateSampleMetrics();

        // Act
        var cut = Render<MetricCardsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics)
            .Add(p => p.Currency, "BRL")
            .Add(p => p.IsLoading, false));

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("R$ 1.000,00"); // Spend formatado
        markup.Should().Contain("4,00x");       // ROAS formatado
        markup.Should().Contain("3,50%");       // CTR formatado
        markup.Should().Contain("vs R$ 800,00"); // Comparativo de período anterior
    }

    /// <summary>
    /// Valida que métricas de custo invertido (CPA e CPC) recebem a classe semântica positiva quando em queda e negativa quando em alta.
    /// </summary>
    [Fact]
    public void MetricCardsGrid_WhenInvertedCostMetric_AppliesCorrectSemanticTrendClass()
    {
        // Arrange: CPA reduziu 20% (bom/positivo), CPC subiu 10% (ruim/negativo)
        var metrics = new List<ExecutiveMetricItemDto>
        {
            new("Cpa", "Custo por Aquisição (CPA)", 20.0m, 25.0m, -20.0m, true, "Currency"),
            new("Cpc", "Custo por Clique (CPC)", 2.20m, 2.00m, 10.0m, false, "Currency")
        };

        // Act
        var cut = Render<MetricCardsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics)
            .Add(p => p.Currency, "BRL")
            .Add(p => p.IsLoading, false));

        // Assert
        var cpaCard = cut.Find("[data-metric='Cpa']");
        var cpaBadge = cpaCard.QuerySelector(".metric-trend-badge");
        cpaBadge.Should().NotBeNull();
        cpaBadge!.ClassList.Should().Contain("trend-positive");

        var cpcCard = cut.Find("[data-metric='Cpc']");
        var cpcBadge = cpcCard.QuerySelector(".metric-trend-badge");
        cpcBadge.Should().NotBeNull();
        cpcBadge!.ClassList.Should().Contain("trend-negative");
    }

    /// <summary>
    /// Valida que quando IsLoading é verdadeiro o grid exibe 6 esqueletos de carregamento animado (skeleton loader).
    /// </summary>
    [Fact]
    public void MetricCardsGrid_WhenLoading_RendersSkeletonLoaders()
    {
        // Act
        var cut = Render<MetricCardsGrid>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        var skeletons = cut.FindAll(".metric-card-skeleton");
        skeletons.Should().HaveCount(6);
    }
}
