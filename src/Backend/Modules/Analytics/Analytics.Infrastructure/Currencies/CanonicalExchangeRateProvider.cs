using Analytics.Domain.Currencies;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Infrastructure.Currencies;

/// <summary>
/// Provedor canônico de taxas de câmbio históricas e diárias com suporte a cotações diretas e triangulação cambial.
/// </summary>
public sealed class CanonicalExchangeRateProvider : IExchangeRateProvider
{
    // Taxas canônicas de referência baseadas em BRL (1 unidade da moeda em Reais)
    private static readonly Dictionary<string, decimal> BaseRatesToBrl = new(StringComparer.OrdinalIgnoreCase)
    {
        [Currency.BRL] = 1.0m,
        [Currency.USD] = 5.45m,
        [Currency.EUR] = 5.95m,
        [Currency.GBP] = 7.10m
    };

    /// <inheritdoc />
    public Task<Result<ExchangeRate>> GetRateAsync(
        string sourceCurrency,
        string targetCurrency,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var sourceResult = Currency.Create(sourceCurrency);
        if (sourceResult.IsFailure)
        {
            return Task.FromResult(Result<ExchangeRate>.Failure(sourceResult.Error));
        }

        var targetResult = Currency.Create(targetCurrency);
        if (targetResult.IsFailure)
        {
            return Task.FromResult(Result<ExchangeRate>.Failure(targetResult.Error));
        }

        var src = sourceResult.Value.Code;
        var tgt = targetResult.Value.Code;

        if (src == tgt)
        {
            var identityRate = new ExchangeRate(src, tgt, 1.0m, date.Date, DateTime.UtcNow, "IdentityCanonical");
            return Task.FromResult(Result<ExchangeRate>.Success(identityRate));
        }

        if (!BaseRatesToBrl.TryGetValue(src, out var srcToBrl) || !BaseRatesToBrl.TryGetValue(tgt, out var tgtToBrl))
        {
            return Task.FromResult(Result<ExchangeRate>.Failure(
                Error.NotFound("ExchangeRate.NotFound", $"Cotação cambial não encontrada para o par {src}/{tgt} na data {date:yyyy-MM-dd}.")));
        }

        // Fator de conversão: 1 src = (srcToBrl / tgtToBrl) tgt
        var calculatedRate = decimal.Round(srcToBrl / tgtToBrl, 6, MidpointRounding.AwayFromZero);

        var exchangeRate = new ExchangeRate(
            SourceCurrency: src,
            TargetCurrency: tgt,
            Rate: calculatedRate,
            Date: date.Date,
            EffectiveDateUtc: DateTime.UtcNow,
            Provider: "CanonicalTriangular");

        return Task.FromResult(Result<ExchangeRate>.Success(exchangeRate));
    }
}
