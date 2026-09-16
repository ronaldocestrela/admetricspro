using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Currencies;

/// <summary>
/// Contrato de provedor de taxas de câmbio históricas e diárias para conversões de moeda.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Obtém a cotação cambial de referência entre a moeda de origem e destino na data indicada.
    /// </summary>
    /// <param name="sourceCurrency">Código ISO da moeda de origem (ex: USD).</param>
    /// <param name="targetCurrency">Código ISO da moeda de destino (ex: BRL).</param>
    /// <param name="date">Data de referência para a cotação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo os detalhes da cotação ou erro de negócio.</returns>
    Task<Result<ExchangeRate>> GetRateAsync(
        string sourceCurrency,
        string targetCurrency,
        DateTime date,
        CancellationToken cancellationToken = default);
}
