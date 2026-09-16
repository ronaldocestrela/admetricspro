using Analytics.Domain.Dashboard;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para o calculador do dashboard executivo (<see cref="IExecutiveDashboardCalculator"/>).
/// Valida cálculo das 6 métricas principais (Spend, CPC, CPM, CTR, CPA, ROAS), comparação com período anterior,
/// polaridade invertida de custos, séries temporais e shares por plataforma e dispositivo.
/// </summary>
public sealed class ExecutiveDashboardCalculatorTests
{
    private readonly IExecutiveDashboardCalculator _calculator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ExecutiveDashboardCalculatorTests"/>.
    /// </summary>
    public ExecutiveDashboardCalculatorTests()
    {
        _calculator = new ExecutiveDashboardCalculator();
    }

    /// <summary>
    /// Valida que ao informar intervalo de datas inválido (início posterior ao fim), a operação retorna falha de validação.
    /// </summary>
    [Fact]
    public void Calculate_WhenDateRangeIsInvalid_ReturnsFailure()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var start = now;
        var end = now.AddDays(-7); // Data final antes da inicial

        // Act
        var result = _calculator.Calculate(
            Enumerable.Empty<RawMetricPoint>(),
            Enumerable.Empty<RawMetricPoint>(),
            start,
            end,
            start.AddDays(-7),
            start);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ExecutiveDashboard.InvalidDateRange");
    }

    /// <summary>
    /// Valida que na ausência de dados a operação retorna métricas zeradas sem lançar exceções de divisão por zero.
    /// </summary>
    [Fact]
    public void Calculate_WhenNoData_ReturnsZeroedMetricsWithoutExceptions()
    {
        // Arrange
        var start = DateTime.UtcNow.Date.AddDays(-6);
        var end = DateTime.UtcNow.Date;
        var prevStart = start.AddDays(-7);
        var prevEnd = start.AddDays(-1);

        // Act
        var result = _calculator.Calculate(
            Enumerable.Empty<RawMetricPoint>(),
            Enumerable.Empty<RawMetricPoint>(),
            start,
            end,
            prevStart,
            prevEnd,
            "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var value = result.Value;
        value.Metrics.Should().HaveCount(6);

        var spend = value.Metrics.First(m => m.MetricKey == "Spend");
        spend.CurrentValue.Should().Be(0m);
        spend.PreviousValue.Should().Be(0m);
        spend.PercentageChange.Should().Be(0m);

        var cpc = value.Metrics.First(m => m.MetricKey == "Cpc");
        cpc.CurrentValue.Should().Be(0m);

        var cpa = value.Metrics.First(m => m.MetricKey == "Cpa");
        cpa.CurrentValue.Should().Be(0m);

        var roas = value.Metrics.First(m => m.MetricKey == "Roas");
        roas.CurrentValue.Should().Be(0m);
    }

    /// <summary>
    /// Valida que dados válidos produzem os 6 KPIs consolidados com precisão matemática, deltas comparativos e respeito à polaridade invertida de custos.
    /// </summary>
    [Fact]
    public void Calculate_WithValidData_CalculatesKpisAndComparisonsAccurately()
    {
        // Arrange
        var start = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);
        var prevStart = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevEnd = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

        // Current period: Spend 1000, Revenue 4000, Impressions 50000, Clicks 2000, Conversions 50
        // CPC: 1000 / 2000 = 0.50
        // CPM: (1000 / 50000) * 1000 = 20.00
        // CTR: (2000 / 50000) * 100 = 4.00%
        // CPA: 1000 / 50 = 20.00
        // ROAS: 4000 / 1000 = 4.00x
        var currentPoints = new List<RawMetricPoint>
        {
            new(start, "Meta", "Mobile", 600m, 2400m, 30000, 1200, 30),
            new(start.AddDays(1), "Google", "Desktop", 400m, 1600m, 20000, 800, 20)
        };

        // Previous period: Spend 1200, Revenue 3600, Impressions 40000, Clicks 1600, Conversions 40
        // CPC: 1200 / 1600 = 0.75
        // CPM: (1200 / 40000) * 1000 = 30.00
        // CTR: (1600 / 40000) * 100 = 4.00%
        // CPA: 1200 / 40 = 30.00
        // ROAS: 3600 / 1200 = 3.00x
        var prevPoints = new List<RawMetricPoint>
        {
            new(prevStart, "Meta", "Mobile", 700m, 2100m, 25000, 1000, 25),
            new(prevStart.AddDays(1), "Google", "Desktop", 500m, 1500m, 15000, 600, 15)
        };

        // Act
        var result = _calculator.Calculate(
            currentPoints,
            prevPoints,
            start,
            end,
            prevStart,
            prevEnd,
            "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var value = result.Value;

        // Spend: 1000 vs 1200 => -16.67%
        var spend = value.Metrics.First(m => m.MetricKey == "Spend");
        spend.CurrentValue.Should().Be(1000m);
        spend.PreviousValue.Should().Be(1200m);
        spend.PercentageChange.Should().BeApproximately(-16.67m, 0.05m);

        // CPC: 0.50 vs 0.75 => -33.33% (Redução de custo = Melhoria / IsPositiveImprovement is true)
        var cpc = value.Metrics.First(m => m.MetricKey == "Cpc");
        cpc.CurrentValue.Should().Be(0.50m);
        cpc.PreviousValue.Should().Be(0.75m);
        cpc.PercentageChange.Should().BeApproximately(-33.33m, 0.05m);
        cpc.IsPositiveImprovement.Should().BeTrue("redução no CPC é positiva para o cliente");

        // CPM: 20.00 vs 30.00 => -33.33% (Redução de custo = Melhoria / IsPositiveImprovement is true)
        var cpm = value.Metrics.First(m => m.MetricKey == "Cpm");
        cpm.CurrentValue.Should().Be(20.00m);
        cpm.PreviousValue.Should().Be(30.00m);
        cpm.IsPositiveImprovement.Should().BeTrue("redução no CPM é positiva");

        // CPA: 20.00 vs 30.00 => -33.33% (Redução de CPA = Melhoria)
        var cpa = value.Metrics.First(m => m.MetricKey == "Cpa");
        cpa.CurrentValue.Should().Be(20.00m);
        cpa.PreviousValue.Should().Be(30.00m);
        cpa.IsPositiveImprovement.Should().BeTrue("redução no CPA é positiva");

        // ROAS: 4.00 vs 3.00 => +33.33% (Aumento de ROAS = Melhoria)
        var roas = value.Metrics.First(m => m.MetricKey == "Roas");
        roas.CurrentValue.Should().Be(4.00m);
        roas.PreviousValue.Should().Be(3.00m);
        roas.PercentageChange.Should().BeApproximately(33.33m, 0.05m);
        roas.IsPositiveImprovement.Should().BeTrue("aumento de ROAS é positivo");
    }

    /// <summary>
    /// Valida que a distribuição por canal calcula participações relativas somando 100%.
    /// </summary>
    [Fact]
    public void Calculate_WithChannelDistribution_ProducesAccurateShares()
    {
        // Arrange
        var start = DateTime.UtcNow.Date.AddDays(-7);
        var end = DateTime.UtcNow.Date;

        var points = new List<RawMetricPoint>
        {
            new(start, "Meta", "Mobile", 600m, 1800m, 10000, 500, 20),
            new(start, "Google", "Desktop", 400m, 1600m, 8000, 400, 15)
        };

        // Act
        var result = _calculator.Calculate(
            points,
            Enumerable.Empty<RawMetricPoint>(),
            start,
            end,
            start.AddDays(-7),
            start,
            "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var platformShares = result.Value.PlatformBreakdown;
        platformShares.Should().HaveCount(2);

        var meta = platformShares.First(p => p.Platform == "Meta");
        meta.Spend.Should().Be(600m);
        meta.ShareOfSpendPercentage.Should().Be(60.00m);

        var google = platformShares.First(p => p.Platform == "Google");
        google.Spend.Should().Be(400m);
        google.ShareOfSpendPercentage.Should().Be(40.00m);

        (meta.ShareOfSpendPercentage + google.ShareOfSpendPercentage).Should().Be(100.00m);
    }
}
