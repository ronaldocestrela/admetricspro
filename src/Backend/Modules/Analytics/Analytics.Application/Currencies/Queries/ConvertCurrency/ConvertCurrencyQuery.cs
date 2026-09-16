using Analytics.Application.Currencies.Dtos;
using Analytics.Domain.Currencies;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Currencies.Queries.ConvertCurrency;

/// <summary>
/// Consulta para conversão cambial individual de valor financeiro entre moedas suportadas.
/// </summary>
/// <param name="Amount">Montante financeiro a ser convertido.</param>
/// <param name="SourceCurrency">Moeda original (ex: USD).</param>
/// <param name="TargetCurrency">Moeda de destino (ex: BRL).</param>
/// <param name="Date">Data da cotação de referência.</param>
public sealed record ConvertCurrencyQuery(
    decimal Amount,
    string SourceCurrency,
    string TargetCurrency,
    DateTime Date) : IQuery<CurrencyConversionDto>;

/// <summary>
/// Manipulador da consulta <see cref="ConvertCurrencyQuery"/>.
/// </summary>
public sealed class ConvertCurrencyQueryHandler : IQueryHandler<ConvertCurrencyQuery, CurrencyConversionDto>
{
    private readonly ICurrencyConverter _currencyConverter;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ConvertCurrencyQueryHandler"/>.
    /// </summary>
    /// <param name="currencyConverter">Serviço de conversão cambial.</param>
    public ConvertCurrencyQueryHandler(ICurrencyConverter currencyConverter)
    {
        _currencyConverter = currencyConverter ?? throw new ArgumentNullException(nameof(currencyConverter));
    }

    /// <inheritdoc />
    public async Task<Result<CurrencyConversionDto>> Handle(
        ConvertCurrencyQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var conversionResult = await _currencyConverter.ConvertAsync(
            query.Amount,
            query.SourceCurrency,
            query.TargetCurrency,
            query.Date,
            cancellationToken);

        if (conversionResult.IsFailure)
        {
            return Result<CurrencyConversionDto>.Failure(conversionResult.Error);
        }

        var r = conversionResult.Value;
        var dto = new CurrencyConversionDto(
            OriginalAmount: r.OriginalAmount,
            ConvertedAmount: r.ConvertedAmount,
            SourceCurrency: r.SourceCurrency,
            TargetCurrency: r.TargetCurrency,
            ExchangeRate: r.ExchangeRate,
            Date: r.Date,
            EffectiveDateUtc: r.EffectiveDateUtc);

        return Result<CurrencyConversionDto>.Success(dto);
    }
}
