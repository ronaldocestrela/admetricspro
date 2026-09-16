namespace Analytics.Domain.Creatives;

/// <summary>
/// Representa o ponto consolidado de métricas diárias de um anúncio criativo,
/// servindo como dado de entrada para cálculo de tendência de CTR e saturação de frequência.
/// </summary>
/// <param name="Date">Data da métrica normalizada.</param>
/// <param name="Impressions">Volume de impressões registradas no dia.</param>
/// <param name="Clicks">Volume de cliques registrados no dia.</param>
/// <param name="Spend">Total investido no dia na moeda local.</param>
/// <param name="Conversions">Total de conversões registradas no dia.</param>
/// <param name="ConversionValue">Receita financeira direta auferida no dia.</param>
/// <param name="Frequency">Frequência média de exibição do criativo no dia (ex: 1.25).</param>
public sealed record CreativeDailyMetricPoint(
    DateTime Date,
    long Impressions,
    long Clicks,
    decimal Spend,
    decimal Conversions,
    decimal ConversionValue,
    decimal Frequency)
{
    /// <summary>
    /// Taxa de cliques (CTR) diária calculada em percentual (ex: 2.50 para 2.50%).
    /// </summary>
    public decimal Ctr => Impressions > 0
        ? Math.Round(((decimal)Clicks / Impressions) * 100m, 4)
        : 0m;

    /// <summary>
    /// Custo por Clique (CPC) médio do dia.
    /// </summary>
    public decimal Cpc => Clicks > 0
        ? Math.Round(Spend / Clicks, 4)
        : 0m;

    /// <summary>
    /// Custo por Aquisição (CPA) médio do dia.
    /// </summary>
    public decimal Cpa => Conversions > 0
        ? Math.Round(Spend / Conversions, 4)
        : 0m;

    /// <summary>
    /// Retorno sobre Investimento Publicitário (ROAS) do dia.
    /// </summary>
    public decimal Roas => Spend > 0
        ? Math.Round(ConversionValue / Spend, 4)
        : 0m;
}
