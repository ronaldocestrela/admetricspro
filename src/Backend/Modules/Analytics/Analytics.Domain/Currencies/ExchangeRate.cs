namespace Analytics.Domain.Currencies;

/// <summary>
/// Representa a cotação cambial apurada entre duas moedas em uma data de referência específica.
/// </summary>
/// <param name="SourceCurrency">Código ISO da moeda de origem.</param>
/// <param name="TargetCurrency">Código ISO da moeda de destino.</param>
/// <param name="Rate">Fator multiplicador de conversão cambial (1 unidade de Source = Rate unidades de Target).</param>
/// <param name="Date">Data da cotação (meia-noite UTC).</param>
/// <param name="EffectiveDateUtc">Carimbo de data/hora UTC em que a cotação foi apurada ou registrada.</param>
/// <param name="Provider">Nome do provedor ou fonte de dados cambiais.</param>
public sealed record ExchangeRate(
    string SourceCurrency,
    string TargetCurrency,
    decimal Rate,
    DateTime Date,
    DateTime EffectiveDateUtc,
    string Provider);
