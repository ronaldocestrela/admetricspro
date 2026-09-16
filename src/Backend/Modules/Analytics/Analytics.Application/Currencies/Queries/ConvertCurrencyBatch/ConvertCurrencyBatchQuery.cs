using Analytics.Application.Currencies.Dtos;
using Analytics.Domain.Currencies;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Currencies.Queries.ConvertCurrencyBatch;

/// <summary>
/// Solicitação de item individual para conversão cambial em lote.
/// </summary>
/// <param name="Amount">Montante financeiro.</param>
/// <param name="SourceCurrency">Moeda de origem.</param>
/// <param name="Date">Data da cotação.</param>
public sealed record CurrencyBatchItemInput(
    decimal Amount,
    string SourceCurrency,
    DateTime Date);

/// <summary>
/// Consulta para conversão cambial em lote de múltiplos valores para uma moeda destino única.
/// </summary>
/// <param name="Items">Coleção de itens a serem convertidos.</param>
/// <param name="TargetCurrency">Moeda destino unificada.</param>
public sealed record ConvertCurrencyBatchQuery(
    IReadOnlyList<CurrencyBatchItemInput> Items,
    string TargetCurrency) : IQuery<IReadOnlyList<CurrencyConversionDto>>;

/// <summary>
/// Manipulador da consulta <see cref="ConvertCurrencyBatchQuery"/>.
/// </summary>
public sealed class ConvertCurrencyBatchQueryHandler : IQueryHandler<ConvertCurrencyBatchQuery, IReadOnlyList<CurrencyConversionDto>>
{
    private readonly ICurrencyConverter _currencyConverter;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ConvertCurrencyBatchQueryHandler"/>.
    /// </summary>
    /// <param name="currencyConverter">Serviço de conversão cambial.</param>
    public ConvertCurrencyBatchQueryHandler(ICurrencyConverter currencyConverter)
    {
        _currencyConverter = currencyConverter ?? throw new ArgumentNullException(nameof(currencyConverter));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CurrencyConversionDto>>> Handle(
        ConvertCurrencyBatchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Items is null || query.Items.Count == 0)
        {
            return Result<IReadOnlyList<CurrencyConversionDto>>.Success(Array.Empty<CurrencyConversionDto>());
        }

        var requests = query.Items.Select(i => new CurrencyConversionRequest(i.Amount, i.SourceCurrency, i.Date));

        var batchResult = await _currencyConverter.ConvertBatchAsync(requests, query.TargetCurrency, cancellationToken);
        if (batchResult.IsFailure)
        {
            return Result<IReadOnlyList<CurrencyConversionDto>>.Failure(batchResult.Error);
        }

        var dtos = batchResult.Value.Select(r => new CurrencyConversionDto(
            OriginalAmount: r.OriginalAmount,
            ConvertedAmount: r.ConvertedAmount,
            SourceCurrency: r.SourceCurrency,
            TargetCurrency: r.TargetCurrency,
            ExchangeRate: r.ExchangeRate,
            Date: r.Date,
            EffectiveDateUtc: r.EffectiveDateUtc)).ToList();

        return Result<IReadOnlyList<CurrencyConversionDto>>.Success(dtos.AsReadOnly());
    }
}
