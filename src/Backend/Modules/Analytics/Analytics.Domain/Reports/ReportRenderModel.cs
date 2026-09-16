using BuildingBlocks.Domain.Reports;

namespace Analytics.Domain.Reports;

/// <summary>
/// Modelo consolidado com todas as informações necessárias para renderizar o relatório em PDF e página interativa.
/// </summary>
public sealed class ReportRenderModel
{
    /// <summary>
    /// Título do relatório.
    /// </summary>
    public string ReportTitle { get; init; } = string.Empty;

    /// <summary>
    /// Nome do Workspace (cliente da agência).
    /// </summary>
    public string WorkspaceName { get; init; } = string.Empty;

    /// <summary>
    /// Data inicial do período de métricas analisado.
    /// </summary>
    public DateTime DateRangeStart { get; init; }

    /// <summary>
    /// Data final do período de métricas analisado.
    /// </summary>
    public DateTime DateRangeEnd { get; init; }

    /// <summary>
    /// Snapshot com a identidade visual da agência parceira.
    /// </summary>
    public ReportBrandingSnapshot Branding { get; init; } = ReportBrandingSnapshot.Default;

    /// <summary>
    /// Resumo executivo dos KPIs consolidados de investimento e conversão.
    /// </summary>
    public ReportKpiSummary KpiSummary { get; init; } = new();

    /// <summary>
    /// Distribuição de desempenho dividida por plataforma de anúncios.
    /// </summary>
    public IReadOnlyList<ReportChannelMetric> ChannelBreakdown { get; init; } = Array.Empty<ReportChannelMetric>();

    /// <summary>
    /// Peças de mídia com melhor desempenho no período e alertas de fadiga.
    /// </summary>
    public IReadOnlyList<ReportTopCreative> TopCreatives { get; init; } = Array.Empty<ReportTopCreative>();

    /// <summary>
    /// Diagnósticos sintetizados do Copiloto de IA para a conta.
    /// </summary>
    public IReadOnlyList<string> CopilotInsights { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Observações e notas estratégicas adicionadas pelo gestor da conta.
    /// </summary>
    public string? CustomNotes { get; init; }

    /// <summary>
    /// Data e hora da compilação do relatório.
    /// </summary>
    public DateTime GeneratedAt { get; init; }

    /// <summary>
    /// URL pública e segura para visualização interativa do relatório.
    /// </summary>
    public string? ShareUrl { get; init; }
}

/// <summary>
/// KPIs executivos de topo de funil e conversão financeira do relatório.
/// </summary>
public sealed class ReportKpiSummary
{
    /// <summary>
    /// Valor monetário total investido nas campanhas.
    /// </summary>
    public decimal TotalSpend { get; init; }

    /// <summary>
    /// Valor monetário bruto gerado em vendas/conversões.
    /// </summary>
    public decimal TotalRevenue { get; init; }

    /// <summary>
    /// Retorno sobre investimento em publicidade unificado (Revenue / Spend).
    /// </summary>
    public decimal BlendedRoas { get; init; }

    /// <summary>
    /// Custo por aquisição consolidado (Spend / Conversions).
    /// </summary>
    public decimal BlendedCpa { get; init; }

    /// <summary>
    /// Total de conversões registradas.
    /// </summary>
    public int TotalConversions { get; init; }

    /// <summary>
    /// Total de cliques nos anúncios.
    /// </summary>
    public int TotalClicks { get; init; }

    /// <summary>
    /// Total de impressões exibidas.
    /// </summary>
    public long TotalImpressions { get; init; }
}

/// <summary>
/// Desempenho sumarizado de uma plataforma de mídia específica (Meta, Google, Bing, TikTok).
/// </summary>
public sealed class ReportChannelMetric
{
    /// <summary>
    /// Nome da plataforma (ex: MetaAds, GoogleAds, TikTokAds, BingAds).
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Investimento realizado na plataforma.
    /// </summary>
    public decimal Spend { get; init; }

    /// <summary>
    /// Receita atribuída à plataforma.
    /// </summary>
    public decimal Revenue { get; init; }

    /// <summary>
    /// ROAS da plataforma.
    /// </summary>
    public decimal Roas { get; init; }

    /// <summary>
    /// Conversões obtidas na plataforma.
    /// </summary>
    public int Conversions { get; init; }

    /// <summary>
    /// Porcentagem da verba total alocada nesta plataforma (0 a 100).
    /// </summary>
    public decimal SharePercentage { get; init; }
}

/// <summary>
/// Destaque de criativo de alto impacto no período analisado.
/// </summary>
public sealed class ReportTopCreative
{
    /// <summary>
    /// Nome do anúncio ou criativo.
    /// </summary>
    public string AdName { get; init; } = string.Empty;

    /// <summary>
    /// Canal de mídia em que o anúncio foi veiculado.
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Investimento acumulado no anúncio.
    /// </summary>
    public decimal Spend { get; init; }

    /// <summary>
    /// Taxa de cliques (CTR%).
    /// </summary>
    public decimal Ctr { get; init; }

    /// <summary>
    /// ROAS obtido pelo anúncio.
    /// </summary>
    public decimal Roas { get; init; }

    /// <summary>
    /// Status da fadiga do criativo (ex: "Saudável", "Fadiga Leve", "Fadiga Crítica").
    /// </summary>
    public string FatigueStatus { get; init; } = "Saudável";
}
