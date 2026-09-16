namespace Analytics.Domain.Dashboard;

/// <summary>
/// Registro atômico de métrica consolidada por data, canal e dispositivo para processamento analítico do dashboard.
/// </summary>
/// <param name="Date">Data do registro em UTC.</param>
/// <param name="Platform">Plataforma de anúncios (ex.: Meta, Google, TikTok, Bing).</param>
/// <param name="Device">Dispositivo de acesso (ex.: Mobile, Desktop, Tablet).</param>
/// <param name="Spend">Investimento monetário.</param>
/// <param name="Revenue">Receita ou valor de conversão gerado.</param>
/// <param name="Impressions">Número de impressões.</param>
/// <param name="Clicks">Número de cliques.</param>
/// <param name="Conversions">Total de conversões.</param>
public sealed record RawMetricPoint(
    DateTime Date,
    string Platform,
    string Device,
    decimal Spend,
    decimal Revenue,
    long Impressions,
    long Clicks,
    decimal Conversions);
