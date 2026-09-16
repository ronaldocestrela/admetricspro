namespace Analytics.Domain.Currencies;

/// <summary>
/// Solicitação individual de conversão cambial parametrizada por valor, moeda de origem e data.
/// </summary>
/// <param name="Amount">Valor financeiro a ser convertido.</param>
/// <param name="SourceCurrency">Código ISO da moeda original do valor.</param>
/// <param name="Date">Data de referência histórica ou corrente para obtenção da cotação.</param>
public sealed record CurrencyConversionRequest(
    decimal Amount,
    string SourceCurrency,
    DateTime Date);
