using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.Meta;

/// <summary>Filtros de consulta compatíveis com Insights; datas representam dias no fuso da conta.</summary>
/// <param name="Since">Primeiro dia.</param>
/// <param name="Until">Último dia.</param>
/// <param name="Level">account, campaign, adset ou ad.</param>
/// <param name="Breakdown">Um breakdown suportado, opcional.</param>
/// <param name="ConversionAction">Tipo de ação explícito para conversões e CPA.</param>
/// <param name="CampaignId">Filtro externo de campanha.</param>
/// <param name="AdSetId">Filtro externo de conjunto.</param>
/// <param name="AdId">Filtro externo de anúncio.</param>
public sealed record MetaInsightsFilter(DateOnly Since, DateOnly Until, string Level = "campaign", string? Breakdown = null,
    string? ConversionAction = null, string? CampaignId = null, string? AdSetId = null, string? AdId = null);

/// <summary>Linha de Insights; ausência de métricas é preservada como nulo.</summary>
public sealed record MetaInsight
{
    /// <summary>Primeiro dia da linha, no fuso da conta.</summary>
    public DateOnly DateStart { get; init; }
    /// <summary>Último dia da linha, no fuso da conta.</summary>
    public DateOnly DateStop { get; init; }
    /// <summary>Campanha externa.</summary>
    public string? CampaignId { get; init; }
    /// <summary>Conjunto externo.</summary>
    public string? AdSetId { get; init; }
    /// <summary>Anúncio externo.</summary>
    public string? AdId { get; init; }
    /// <summary>Investimento original, sem conversão de moeda.</summary>
    public decimal? Spend { get; init; }
    /// <summary>Impressões.</summary>
    public long? Impressions { get; init; }
    /// <summary>Alcance único da linha; nunca somar entre linhas.</summary>
    public long? Reach { get; init; }
    /// <summary>Cliques totais.</summary>
    public long? Clicks { get; init; }
    /// <summary>Cliques no link.</summary>
    public long? LinkClicks { get; init; }
    /// <summary>CPC informado pela Meta.</summary>
    public decimal? Cpc { get; init; }
    /// <summary>CPM informado pela Meta.</summary>
    public decimal? Cpm { get; init; }
    /// <summary>CTR informado pela Meta.</summary>
    public decimal? Ctr { get; init; }
    /// <summary>Frequência da linha; não aditiva.</summary>
    public decimal? Frequency { get; init; }
    /// <summary>Ação explicitamente escolhida para medir conversões.</summary>
    public string? ConversionAction { get; init; }
    /// <summary>Conversões do tipo de ação escolhido, se disponíveis.</summary>
    public decimal? Conversions { get; init; }
    /// <summary>Valor das conversões do mesmo tipo de ação, se disponível.</summary>
    public decimal? ConversionValue { get; init; }
    /// <summary>CPA para o evento explícito, ou nulo quando indisponível.</summary>
    public decimal? Cpa { get; init; }
    /// <summary>ROAS somente quando há valor de conversão fornecido pela Meta.</summary>
    public decimal? Roas { get; init; }
    /// <summary>Ações preservadas por tipo; nunca somadas como compras.</summary>
    public IReadOnlyDictionary<string, decimal> Actions { get; init; } = new Dictionary<string, decimal>();
    /// <summary>Custos por ação fornecidos pela Meta.</summary>
    public IReadOnlyDictionary<string, decimal> CostPerAction { get; init; } = new Dictionary<string, decimal>();
    /// <summary>Métricas de vídeo por tipo.</summary>
    public IReadOnlyDictionary<string, decimal?> VideoMetrics { get; init; } = new Dictionary<string, decimal?>();
    /// <summary>Nome do breakdown solicitado.</summary>
    public string? Breakdown { get; init; }
    /// <summary>Valor do breakdown desta linha.</summary>
    public string? BreakdownValue { get; init; }
}

/// <summary>Consulta detalhada e paginada de Insights.</summary>
public interface IMetaInsightsApi
{
    /// <summary>Consulta somente a conta já autorizada pelo chamador.</summary>
    /// <param name="externalAccountId">ID canônico da conta vinculada.</param>
    /// <param name="token">Token backend da autorização.</param>
    /// <param name="filter">Filtros validados.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linhas tipadas com métricas opcionais.</returns>
    Task<Result<IReadOnlyList<MetaInsight>>> ReadAsync(string externalAccountId, string token, MetaInsightsFilter filter, CancellationToken cancellationToken);
}
