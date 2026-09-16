namespace Analytics.Domain.Copilot;

/// <summary>
/// Modelo de entrada representando o desempenho de uma palavra-chave ou termo de pesquisa veiculado em canais de busca.
/// </summary>
public sealed record SearchKeywordPerformance
{
    /// <summary>
    /// Canal de mídia (GoogleAds ou BingAds).
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Identificador único da campanha.
    /// </summary>
    public Guid CampaignId { get; init; }

    /// <summary>
    /// Nome da campanha.
    /// </summary>
    public string CampaignName { get; init; } = string.Empty;

    /// <summary>
    /// Identificador único do grupo de anúncios.
    /// </summary>
    public Guid AdGroupId { get; init; }

    /// <summary>
    /// Nome do grupo de anúncios.
    /// </summary>
    public string AdGroupName { get; init; } = string.Empty;

    /// <summary>
    /// Palavra-chave ou termo de pesquisa.
    /// </summary>
    public string Keyword { get; init; } = string.Empty;

    /// <summary>
    /// Tipo de correspondência (Exact, Phrase, Broad).
    /// </summary>
    public string MatchType { get; init; } = string.Empty;

    /// <summary>
    /// Valor total investido no termo.
    /// </summary>
    public decimal Spend { get; init; }

    /// <summary>
    /// Quantidade de cliques obtidos.
    /// </summary>
    public long Clicks { get; init; }

    /// <summary>
    /// Quantidade de conversões geradas.
    /// </summary>
    public decimal Conversions { get; init; }

    /// <summary>
    /// Custo por Clique médio (CPC).
    /// </summary>
    public decimal Cpc { get; init; }

    /// <summary>
    /// Custo por Aquisição / Conversão (CPA).
    /// </summary>
    public decimal Cpa { get; init; }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SearchKeywordPerformance"/>.
    /// </summary>
    public SearchKeywordPerformance(
        string platform,
        Guid campaignId,
        string campaignName,
        Guid adGroupId,
        string adGroupName,
        string keyword,
        string matchType,
        decimal spend,
        long clicks,
        decimal conversions,
        decimal cpc,
        decimal cpa)
    {
        Platform = platform;
        CampaignId = campaignId;
        CampaignName = campaignName;
        AdGroupId = adGroupId;
        AdGroupName = adGroupName;
        Keyword = keyword;
        MatchType = matchType;
        Spend = spend;
        Clicks = clicks;
        Conversions = conversions;
        Cpc = cpc;
        Cpa = cpa;
    }
}
