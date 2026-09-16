using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Currencies;

/// <summary>
/// Contrato do serviço central de conversão cambial com suporte a normalização e cache local.
/// </summary>
public interface ICurrencyConverter
{
    /// <summary>
    /// Converte um montante monetário de uma moeda de origem para uma moeda de destino considerando a cotação da data.
    /// </summary>
    /// <param name="amount">Valor original a ser convertido.</param>
    /// <param name="sourceCurrency">Código ISO da moeda de origem.</param>
    /// <param name="targetCurrency">Código ISO da moeda de destino.</param>
    /// <param name="date">Data de referência para a cotação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo o cálculo da conversão cambial ou erro de validação/negócio.</returns>
    Task<Result<CurrencyConversionResult>> ConvertAsync(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converte em lote múltiplos valores monetários heterogêneos para uma moeda de destino única.
    /// </summary>
    /// <param name="requests">Coleção de solicitações de conversão com valor, moeda de origem e data.</param>
    /// <param name="targetCurrency">Código ISO da moeda de destino unificada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo a lista ordenada de conversões realizadas ou erro de validação.</returns>
    Task<Result<IReadOnlyList<CurrencyConversionResult>>> ConvertBatchAsync(
        IEnumerable<CurrencyConversionRequest> requests,
        string targetCurrency,
        CancellationToken cancellationToken = default);
}
