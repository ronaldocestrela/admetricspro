using Analytics.Domain.Currencies;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Caching.Memory;

namespace Analytics.Infrastructure.Currencies;

/// <summary>
/// Serviço de conversão cambial com suporte a normalização, cache local em dois níveis e tolerância a falhas.
/// </summary>
public sealed class CurrencyConverter : ICurrencyConverter
{
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IMemoryCache _memoryCache;

    private static readonly TimeSpan HistoricalCacheDuration = TimeSpan.FromDays(7);
    private static readonly TimeSpan TodayCacheDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CurrencyConverter"/>.
    /// </summary>
    /// <param name="exchangeRateProvider">Provedor de taxas de câmbio de referência.</param>
    /// <param name="memoryCache">Mecanismo de cache local em memória.</param>
    public CurrencyConverter(
        IExchangeRateProvider exchangeRateProvider,
        IMemoryCache memoryCache)
    {
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <inheritdoc />
    public async Task<Result<CurrencyConversionResult>> ConvertAsync(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        if (amount < 0)
        {
            return Result<CurrencyConversionResult>.Failure(
                Error.Validation("CurrencyConverter.NegativeAmount", "O montante a converter não pode ser negativo."));
        }

        var sourceResult = Currency.Create(sourceCurrency);
        if (sourceResult.IsFailure)
        {
            return Result<CurrencyConversionResult>.Failure(sourceResult.Error);
        }

        var targetResult = Currency.Create(targetCurrency);
        if (targetResult.IsFailure)
        {
            return Result<CurrencyConversionResult>.Failure(targetResult.Error);
        }

        var src = sourceResult.Value.Code;
        var tgt = targetResult.Value.Code;
        var normalizedDate = date.Date;

        // Mesma moeda: retorno imediato com taxa 1.0 sem consulta a cache ou provedor
        if (src == tgt)
        {
            return Result<CurrencyConversionResult>.Success(new CurrencyConversionResult(
                OriginalAmount: amount,
                ConvertedAmount: amount,
                SourceCurrency: src,
                TargetCurrency: tgt,
                ExchangeRate: 1.0m,
                Date: normalizedDate,
                EffectiveDateUtc: DateTime.UtcNow));
        }

        var cacheKey = $"fx_{src}_{tgt}_{normalizedDate:yyyyMMdd}";

        if (!_memoryCache.TryGetValue(cacheKey, out ExchangeRate? rate) || rate is null)
        {
            var rateResult = await _exchangeRateProvider.GetRateAsync(src, tgt, normalizedDate, cancellationToken);
            if (rateResult.IsFailure)
            {
                return Result<CurrencyConversionResult>.Failure(rateResult.Error);
            }

            rate = rateResult.Value;

            // Determina tempo de cache: datas passadas são imutáveis; dia corrente tem expiração mais curta
            var isToday = normalizedDate == DateTime.UtcNow.Date;
            var cacheDuration = isToday ? TodayCacheDuration : HistoricalCacheDuration;

            _memoryCache.Set(cacheKey, rate, cacheDuration);
        }

        var convertedAmount = decimal.Round(amount * rate.Rate, 4, MidpointRounding.AwayFromZero);

        var result = new CurrencyConversionResult(
            OriginalAmount: amount,
            ConvertedAmount: convertedAmount,
            SourceCurrency: src,
            TargetCurrency: tgt,
            ExchangeRate: rate.Rate,
            Date: rate.Date,
            EffectiveDateUtc: rate.EffectiveDateUtc);

        return Result<CurrencyConversionResult>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CurrencyConversionResult>>> ConvertBatchAsync(
        IEnumerable<CurrencyConversionRequest> requests,
        string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        if (requests is null)
        {
            return Result<IReadOnlyList<CurrencyConversionResult>>.Failure(
                Error.Validation("CurrencyConverter.NullRequests", "A lista de solicitações de conversão não pode ser nula."));
        }

        var targetResult = Currency.Create(targetCurrency);
        if (targetResult.IsFailure)
        {
            return Result<IReadOnlyList<CurrencyConversionResult>>.Failure(targetResult.Error);
        }

        var results = new List<CurrencyConversionResult>();

        foreach (var req in requests)
        {
            var convertResult = await ConvertAsync(req.Amount, req.SourceCurrency, targetResult.Value.Code, req.Date, cancellationToken);
            if (convertResult.IsFailure)
            {
                return Result<IReadOnlyList<CurrencyConversionResult>>.Failure(convertResult.Error);
            }

            results.Add(convertResult.Value);
        }

        return Result<IReadOnlyList<CurrencyConversionResult>>.Success(results.AsReadOnly());
    }
}
