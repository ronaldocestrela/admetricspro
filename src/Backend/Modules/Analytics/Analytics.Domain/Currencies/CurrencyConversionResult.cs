namespace Analytics.Domain.Currencies;

/// <summary>
/// Resultado detalhado de uma conversão cambial processada pelo sistema.
/// </summary>
/// <param name="OriginalAmount">Valor original na moeda de origem.</param>
/// <param name="ConvertedAmount">Valor resultante convertido na moeda de destino.</param>
/// <param name="SourceCurrency">Código ISO da moeda de origem.</param>
/// <param name="TargetCurrency">Código ISO da moeda de destino.</param>
/// <param name="ExchangeRate">Fator multiplicador de cotação utilizado no cálculo.</param>
/// <param name="Date">Data da cotação aplicada.</param>
/// <param name="EffectiveDateUtc">Data e hora UTC da apuração da taxa cambial.</param>
public sealed record CurrencyConversionResult(
    decimal OriginalAmount,
    decimal ConvertedAmount,
    string SourceCurrency,
    string TargetCurrency,
    decimal ExchangeRate,
    DateTime Date,
    DateTime EffectiveDateUtc);
