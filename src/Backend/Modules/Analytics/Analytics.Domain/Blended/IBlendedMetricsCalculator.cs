using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Blended;

/// <summary>
/// Contrato de domínio para o motor de consolidação e cálculo de métricas agregadas multi-canal
/// (MER, Blended ROAS, Blended CAC, CPA, CPC, CPM, CTR e quebra de investimento por plataforma).
/// </summary>
public interface IBlendedMetricsCalculator
{
    /// <summary>
    /// Consolida uma lista de métricas multi-plataforma e heterogêneas em moedas para gerar indicadores agregados.
    /// </summary>
    /// <param name="metrics">Coleção de métricas de veiculação das diferentes redes de anúncios.</param>
    /// <param name="targetCurrency">Moeda destino na qual todos os valores monetários devem ser unificados (ex: BRL, USD).</param>
    /// <param name="totalStoreRevenue">Receita bruta global do e-commerce/loja externa para cálculo do MER estrito (opcional).</param>
    /// <param name="totalNewCustomers">Total de novos clientes adquiridos globalmente no período para cálculo do Blended CAC (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo os indicadores blended ou falha de negócio.</returns>
    Task<Result<BlendedMetricsResult>> CalculateAsync(
        IEnumerable<BlendedMetricInputItem> metrics,
        string targetCurrency,
        decimal? totalStoreRevenue = null,
        int? totalNewCustomers = null,
        CancellationToken cancellationToken = default);
}
