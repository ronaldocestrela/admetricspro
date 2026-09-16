using Analytics.Domain.Currencies;
using Analytics.Infrastructure.Currencies;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para o conversor cambial ICurrencyConverter e validação de cache local.
/// </summary>
public sealed class CurrencyConverterTests
{
    private readonly IExchangeRateProvider _rateProviderMock = Substitute.For<IExchangeRateProvider>();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

    private CurrencyConverter CreateConverter() => new(_rateProviderMock, _memoryCache);

    /// <summary>
    /// Valida que ao converter entre a mesma moeda, o retorno é imediato com taxa 1.0 sem consultar provedor externo.
    /// </summary>
    [Fact]
    public async Task ConvertAsync_WhenSameCurrency_ShouldReturnAmountImmediatelyWithoutQueryingProvider()
    {
        // Arrange
        var converter = CreateConverter();
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await converter.ConvertAsync(150.50m, "BRL", "BRL", date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OriginalAmount.Should().Be(150.50m);
        result.Value.ConvertedAmount.Should().Be(150.50m);
        result.Value.ExchangeRate.Should().Be(1.0m);
        result.Value.SourceCurrency.Should().Be("BRL");
        result.Value.TargetCurrency.Should().Be("BRL");

        await _rateProviderMock.DidNotReceiveWithAnyArgs().GetRateAsync(default!, default!, default);
    }

    /// <summary>
    /// Valida o cálculo correto da conversão cambial com taxa obtida do provedor.
    /// </summary>
    /// <param name="amount">Valor original a converter.</param>
    /// <param name="rate">Taxa cambial simulada.</param>
    /// <param name="expectedConverted">Valor esperado convertido.</param>
    [Theory]
    [InlineData(100.0, 5.50, 550.0)]
    [InlineData(250.0, 5.25, 1312.50)]
    public async Task ConvertAsync_WhenValidUsdToBrl_ShouldCalculateCorrectConvertedAmount(
        double amount,
        double rate,
        double expectedConverted)
    {
        // Arrange
        var converter = CreateConverter();
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var decimalAmount = (decimal)amount;
        var decimalRate = (decimal)rate;
        var decimalExpected = (decimal)expectedConverted;

        _rateProviderMock.GetRateAsync("USD", "BRL", date, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<ExchangeRate>.Success(
                new ExchangeRate("USD", "BRL", decimalRate, date, DateTime.UtcNow, "TestProvider")));

        // Act
        var result = await converter.ConvertAsync(decimalAmount, "USD", "BRL", date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OriginalAmount.Should().Be(decimalAmount);
        result.Value.ExchangeRate.Should().Be(decimalRate);
        result.Value.ConvertedAmount.Should().Be(decimalExpected);
        result.Value.SourceCurrency.Should().Be("USD");
        result.Value.TargetCurrency.Should().Be("BRL");
    }

    /// <summary>
    /// Valida que múltiplas chamadas para a mesma data utilizam o cache local sem onerar o provedor.
    /// </summary>
    [Fact]
    public async Task ConvertAsync_WhenCalledMultipleTimesForSameDate_ShouldUseLocalCache()
    {
        // Arrange
        var converter = CreateConverter();
        var date = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        var rate = 6.10m;

        _rateProviderMock.GetRateAsync("EUR", "BRL", date, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<ExchangeRate>.Success(
                new ExchangeRate("EUR", "BRL", rate, date, DateTime.UtcNow, "TestProvider")));

        // Act - 1st call
        var result1 = await converter.ConvertAsync(100m, "EUR", "BRL", date);
        // Act - 2nd call (same parameters)
        var result2 = await converter.ConvertAsync(200m, "EUR", "BRL", date);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.ConvertedAmount.Should().Be(610m);
        result2.Value.ConvertedAmount.Should().Be(1220m);

        // O provedor subjacente deve ter sido consultado apenas UMA vez graças ao cache local
        await _rateProviderMock.Received(1).GetRateAsync("EUR", "BRL", date, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que montantes negativos retornam erro de validação sem exceções.
    /// </summary>
    [Fact]
    public async Task ConvertAsync_WhenAmountIsNegative_ShouldReturnValidationError()
    {
        // Arrange
        var converter = CreateConverter();
        var date = DateTime.UtcNow;

        // Act
        var result = await converter.ConvertAsync(-10m, "USD", "BRL", date);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CurrencyConverter.NegativeAmount");
    }

    /// <summary>
    /// Valida que moedas inválidas ou não suportadas retornam erro de negócio.
    /// </summary>
    [Fact]
    public async Task ConvertAsync_WhenCurrencyIsUnsupported_ShouldReturnValidationError()
    {
        // Arrange
        var converter = CreateConverter();
        var date = DateTime.UtcNow;

        // Act
        var result = await converter.ConvertAsync(100m, "XYZ", "BRL", date);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Currency.Unsupported");
    }

    /// <summary>
    /// Valida que a conversão em lote converte todos os itens para a moeda alvo.
    /// </summary>
    [Fact]
    public async Task ConvertBatchAsync_ShouldConvertAllItemsToTargetCurrency()
    {
        // Arrange
        var converter = CreateConverter();
        var date1 = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        _rateProviderMock.GetRateAsync("USD", "BRL", date1, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<ExchangeRate>.Success(
                new ExchangeRate("USD", "BRL", 5.40m, date1, DateTime.UtcNow, "TestProvider")));

        _rateProviderMock.GetRateAsync("EUR", "BRL", date2, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<ExchangeRate>.Success(
                new ExchangeRate("EUR", "BRL", 6.00m, date2, DateTime.UtcNow, "TestProvider")));

        var requests = new[]
        {
            new CurrencyConversionRequest(100m, "USD", date1),
            new CurrencyConversionRequest(50m, "EUR", date2),
            new CurrencyConversionRequest(300m, "BRL", date2)
        };

        // Act
        var result = await converter.ConvertBatchAsync(requests, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].ConvertedAmount.Should().Be(540.0m);
        result.Value[1].ConvertedAmount.Should().Be(300.0m);
        result.Value[2].ConvertedAmount.Should().Be(300.0m);
        result.Value[2].ExchangeRate.Should().Be(1.0m);
    }
}
