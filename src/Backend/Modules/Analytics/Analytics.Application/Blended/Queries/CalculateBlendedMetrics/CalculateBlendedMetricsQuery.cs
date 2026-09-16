using Analytics.Application.Blended.Dtos;
using BuildingBlocks.Application.Messaging;

namespace Analytics.Application.Blended.Queries.CalculateBlendedMetrics;

/// <summary>
/// Consulta CQRS para cálculo e consolidação de métricas financeiras multi-canal (MER, Blended ROAS, Blended CAC).
/// </summary>
/// <param name="Items">Lista de métricas de veiculação das plataformas.</param>
/// <param name="TargetCurrency">Código ISO da moeda destino de consolidação (padrão: BRL).</param>
/// <param name="TotalStoreRevenue">Receita total externa da loja ou e-commerce para cálculo de MER estrito (opcional).</param>
/// <param name="TotalNewCustomers">Total global de novos clientes adquiridos no período consolidado (opcional).</param>
public sealed record CalculateBlendedMetricsQuery(
    IReadOnlyList<BlendedMetricItemInput> Items,
    string TargetCurrency = "BRL",
    decimal? TotalStoreRevenue = null,
    int? TotalNewCustomers = null) : IQuery<BlendedMetricsDto>;
