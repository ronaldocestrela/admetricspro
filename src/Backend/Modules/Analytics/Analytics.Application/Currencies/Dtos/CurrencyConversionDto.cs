namespace Analytics.Application.Currencies.Dtos;

/// <summary>
/// DTO de resposta para operações de conversão cambial.
/// </summary>
/// <param name="OriginalAmount">Valor financeiro original.</param>
/// <param name="ConvertedAmount">Valor financeiro convertido na moeda destino.</param>
/// <param name="SourceCurrency">Código ISO da moeda de origem.</param>
/// <param name="TargetCurrency">Código ISO da moeda de destino.</param>
/// <param name="ExchangeRate">Taxa de conversão cambial aplicada.</param>
/// <param name="Date">Data da cotação de referência.</param>
/// <param name="EffectiveDateUtc">Carimbo UTC da apuração da taxa.</param>
public sealed record CurrencyConversionDto(
    decimal OriginalAmount,
    decimal ConvertedAmount,
    string SourceCurrency,
    string TargetCurrency,
    decimal ExchangeRate,
    DateTime Date,
    DateTime EffectiveDateUtc);
