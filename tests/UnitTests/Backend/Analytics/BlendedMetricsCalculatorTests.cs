using Analytics.Domain.Blended;
using Analytics.Domain.Currencies;
using Analytics.Infrastructure.Blended;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para o calculador IBlendedMetricsCalculator cobrindo as fórmulas
/// financeiras de MER, Blended ROAS, Blended CAC e quebra por canal.
/// </summary>
public sealed class BlendedMetricsCalculatorTests
{
    private readonly ICurrencyConverter _currencyConverterMock = Substitute.For<ICurrencyConverter>();

    private BlendedMetricsCalculator CreateCalculator() => new(_currencyConverterMock);

    private void SetupCurrencyConversion(string source, string target, decimal rate)
    {
        _currencyConverterMock.ConvertAsync(
            Arg.Any<decimal>(),
            source,
            target,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var amount = callInfo.ArgAt<decimal>(0);
                var src = callInfo.ArgAt<string>(1);
                var tgt = callInfo.ArgAt<string>(2);
                var date = callInfo.ArgAt<DateTime>(3);
                var converted = amount * rate;
                return Task.FromResult(Result<CurrencyConversionResult>.Success(
                    new CurrencyConversionResult(amount, converted, src, tgt, rate, date, date)));
            });
    }

    /// <summary>
    /// Valida que a fórmula do MER (Marketing Efficiency Ratio) é calculada como Receita Total / Gasto Total.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_WhenStoreRevenueProvided_CalculatesMerCorrectly()
    {
        // Arrange
        SetupCurrencyConversion("BRL", "BRL", 1.0m);
        var calculator = CreateCalculator();

        var metrics = new List<BlendedMetricInputItem>
        {
            new("Meta", Guid.NewGuid(), "meta_1", DateTime.UtcNow, 6000m, "BRL", 100000, 2000, 100, 18000m),
            new("Google", Guid.NewGuid(), "goog_1", DateTime.UtcNow, 4000m, "BRL", 50000, 1500, 80, 12000m)
        };

        decimal storeRevenue = 50000m; // Receita bruta global do e-commerce
        int newCustomers = 125;

        // Act
        var result = await calculator.CalculateAsync(metrics, "BRL", storeRevenue, newCustomers);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Value;
        data.TotalSpend.Should().Be(10000m);
        data.TotalConversionValue.Should().Be(30000m);
        data.TotalStoreRevenue.Should().Be(50000m);

        // MER = 50000 / 10000 = 5.0
        data.MarketingEfficiencyRatio.Should().Be(5.0m);

        // Blended ROAS = 30000 / 10000 = 3.0
        data.BlendedRoas.Should().Be(3.0m);

        // Blended CAC = 10000 / 125 = 80.0
        data.BlendedCac.Should().Be(80.0m);

        // Blended CPA = 10000 / 180 = 55.56
        data.BlendedCpa.Should().BeApproximately(55.56m, 0.01m);
    }

    /// <summary>
    /// Valida que o MER utiliza a receita consolidada de conversões quando a receita da loja não for informada.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_WhenStoreRevenueNull_UsesConversionValueForMer()
    {
        // Arrange
        SetupCurrencyConversion("BRL", "BRL", 1.0m);
        var calculator = CreateCalculator();

        var metrics = new List<BlendedMetricInputItem>
        {
            new("Meta", Guid.NewGuid(), "meta_1", DateTime.UtcNow, 5000m, "BRL", 80000, 1000, 50, 15000m),
            new("TikTok", Guid.NewGuid(), "tik_1", DateTime.UtcNow, 5000m, "BRL", 120000, 2000, 75, 10000m)
        };

        // Act
        var result = await calculator.CalculateAsync(metrics, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Value;
        data.TotalSpend.Should().Be(10000m);
        data.TotalConversionValue.Should().Be(25000m);
        data.TotalStoreRevenue.Should().BeNull();

        // MER fallback para TotalConversionValue: 25000 / 10000 = 2.5
        data.MarketingEfficiencyRatio.Should().Be(2.5m);
        data.BlendedRoas.Should().Be(2.5m);
    }

    /// <summary>
    /// Valida a conversão monetária multi-moeda integrada para a moeda destino solicitada.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_WithHeterogeneousCurrencies_ConvertsAllToTargetCurrency()
    {
        // Arrange
        var calculator = CreateCalculator();
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Mock: USD -> BRL (5.50), EUR -> BRL (6.00), BRL -> BRL (1.00)
        _currencyConverterMock.ConvertAsync(Arg.Any<decimal>(), "USD", "BRL", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<CurrencyConversionResult>.Success(
                new CurrencyConversionResult(call.ArgAt<decimal>(0), call.ArgAt<decimal>(0) * 5.50m, "USD", "BRL", 5.50m, date, date))));

        _currencyConverterMock.ConvertAsync(Arg.Any<decimal>(), "EUR", "BRL", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<CurrencyConversionResult>.Success(
                new CurrencyConversionResult(call.ArgAt<decimal>(0), call.ArgAt<decimal>(0) * 6.00m, "EUR", "BRL", 6.00m, date, date))));

        _currencyConverterMock.ConvertAsync(Arg.Any<decimal>(), "BRL", "BRL", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Result<CurrencyConversionResult>.Success(
                new CurrencyConversionResult(call.ArgAt<decimal>(0), call.ArgAt<decimal>(0), "BRL", "BRL", 1.00m, date, date))));

        var metrics = new List<BlendedMetricInputItem>
        {
            // USD 1000 spend -> BRL 5500, USD 3000 conv -> BRL 16500
            new("Meta", Guid.NewGuid(), "meta_usd", date, 1000m, "USD", 50000, 1000, 50, 3000m),
            // EUR 500 spend -> BRL 3000, EUR 1000 conv -> BRL 6000
            new("TikTok", Guid.NewGuid(), "tik_eur", date, 500m, "EUR", 30000, 800, 30, 1000m),
            // BRL 1500 spend -> BRL 1500, BRL 4500 conv -> BRL 4500
            new("Google", Guid.NewGuid(), "goog_brl", date, 1500m, "BRL", 20000, 600, 20, 4500m)
        };

        // Act
        var result = await calculator.CalculateAsync(metrics, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Value;
        // Total Spend = 5500 + 3000 + 1500 = 10000 BRL
        data.TotalSpend.Should().Be(10000m);
        // Total Conv = 16500 + 6000 + 4500 = 27000 BRL
        data.TotalConversionValue.Should().Be(27000m);
        // Blended ROAS = 27000 / 10000 = 2.7
        data.BlendedRoas.Should().Be(2.7m);
        data.TargetCurrency.Should().Be("BRL");
    }

    /// <summary>
    /// Valida que cenários com gasto zero ou conversões zero são tratados com segurança sem exceções de divisão por zero.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_WhenZeroSpendAndConversions_ReturnsZeroRatesSafely()
    {
        // Arrange
        SetupCurrencyConversion("BRL", "BRL", 1.0m);
        var calculator = CreateCalculator();

        var metrics = new List<BlendedMetricInputItem>
        {
            new("Meta", Guid.NewGuid(), "meta_zero", DateTime.UtcNow, 0m, "BRL", 0, 0, 0, 0m)
        };

        // Act
        var result = await calculator.CalculateAsync(metrics, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Value;
        data.TotalSpend.Should().Be(0m);
        data.TotalConversionValue.Should().Be(0m);
        data.MarketingEfficiencyRatio.Should().Be(0m);
        data.BlendedRoas.Should().Be(0m);
        data.BlendedCac.Should().Be(0m);
        data.BlendedCpa.Should().Be(0m);
        data.BlendedCpc.Should().Be(0m);
        data.BlendedCpm.Should().Be(0m);
        data.BlendedCtr.Should().Be(0m);
    }

    /// <summary>
    /// Valida o detalhamento analítico por canal e a soma das participações de investimento em 100%.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_ChannelBreakdown_CalculatesSharesAndIndividualMetrics()
    {
        // Arrange
        SetupCurrencyConversion("BRL", "BRL", 1.0m);
        var calculator = CreateCalculator();

        var metrics = new List<BlendedMetricInputItem>
        {
            new("Meta", Guid.NewGuid(), "m1", DateTime.UtcNow, 7000m, "BRL", 70000, 1400, 70, 21000m),
            new("Google", Guid.NewGuid(), "g1", DateTime.UtcNow, 3000m, "BRL", 30000, 600, 30, 12000m)
        };

        // Act
        var result = await calculator.CalculateAsync(metrics, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var breakdowns = result.Value.ChannelBreakdowns;
        breakdowns.Should().HaveCount(2);

        var meta = breakdowns.First(b => b.Platform == "Meta");
        meta.Spend.Should().Be(7000m);
        meta.SpendSharePercentage.Should().Be(70.0m);
        meta.Roas.Should().Be(3.0m); // 21000 / 7000
        meta.Cpa.Should().Be(100.0m); // 7000 / 70
        meta.Cpc.Should().Be(5.0m); // 7000 / 1400
        meta.Ctr.Should().Be(2.0m); // (1400 / 70000) * 100

        var google = breakdowns.First(b => b.Platform == "Google");
        google.Spend.Should().Be(3000m);
        google.SpendSharePercentage.Should().Be(30.0m);
        google.Roas.Should().Be(4.0m); // 12000 / 3000

        (meta.SpendSharePercentage + google.SpendSharePercentage).Should().Be(100.0m);
    }

    /// <summary>
    /// Valida que valores negativos disparam falha sem exceções pelo padrão Result.
    /// </summary>
    [Fact]
    public async Task CalculateAsync_WhenSpendIsNegative_ReturnsFailureResult()
    {
        // Arrange
        var calculator = CreateCalculator();
        var metrics = new List<BlendedMetricInputItem>
        {
            new("Meta", Guid.NewGuid(), "m_neg", DateTime.UtcNow, -100m, "BRL", 100, 10, 1, 50m)
        };

        // Act
        var result = await calculator.CalculateAsync(metrics, "BRL");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BlendedMetrics.InvalidAmount");
    }

    /// <summary>
    /// Valida que moeda de destino vazia retorna falha de validação.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID_CURRENCY")]
    public async Task CalculateAsync_WhenTargetCurrencyInvalid_ReturnsFailureResult(string targetCurrency)
    {
        // Arrange
        var calculator = CreateCalculator();
        var metrics = new List<BlendedMetricInputItem>();

        // Act
        var result = await calculator.CalculateAsync(metrics, targetCurrency);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BlendedMetrics.InvalidCurrency");
    }
}
