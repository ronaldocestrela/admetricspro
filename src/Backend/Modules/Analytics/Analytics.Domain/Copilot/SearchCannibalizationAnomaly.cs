namespace Analytics.Domain.Copilot;

/// <summary>
/// Anomalia analítica que representa disputa e canibalização de termos de busca entre canais (Google Ads vs. Bing Ads) ou campanhas internas.
/// </summary>
public sealed record SearchCannibalizationAnomaly
{
    /// <summary>
    /// Termo de busca ou palavra-chave em concorrência.
    /// </summary>
    public string SearchTerm { get; init; }

    /// <summary>
    /// Primeiro canal analisado (normalmente GoogleAds).
    /// </summary>
    public string ChannelA { get; init; }

    /// <summary>
    /// Identificador da campanha no Canal A.
    /// </summary>
    public Guid CampaignIdA { get; init; }

    /// <summary>
    /// Nome da campanha no Canal A.
    /// </summary>
    public string CampaignNameA { get; init; }

    /// <summary>
    /// CPC registrado no Canal A.
    /// </summary>
    public decimal CpcA { get; init; }

    /// <summary>
    /// CPA registrado no Canal A.
    /// </summary>
    public decimal CpaA { get; init; }

    /// <summary>
    /// Gasto total no Canal A.
    /// </summary>
    public decimal SpendA { get; init; }

    /// <summary>
    /// Segundo canal analisado (normalmente BingAds ou campanha concorrente).
    /// </summary>
    public string ChannelB { get; init; }

    /// <summary>
    /// Identificador da campanha no Canal B.
    /// </summary>
    public Guid CampaignIdB { get; init; }

    /// <summary>
    /// Nome da campanha no Canal B.
    /// </summary>
    public string CampaignNameB { get; init; }

    /// <summary>
    /// CPC registrado no Canal B.
    /// </summary>
    public decimal CpcB { get; init; }

    /// <summary>
    /// CPA registrado no Canal B.
    /// </summary>
    public decimal CpaB { get; init; }

    /// <summary>
    /// Gasto total no Canal B.
    /// </summary>
    public decimal SpendB { get; init; }

    /// <summary>
    /// Razão de disparidade de custo (ex.: 2.5x mais caro em um canal do que no outro).
    /// </summary>
    public decimal DisparityRatio { get; init; }

    /// <summary>
    /// Estimativa de desperdício financeiro mensal gerado pela concorrência desbalanceada.
    /// </summary>
    public decimal EstimatedMonthlyWastedSpend { get; init; }

    /// <summary>
    /// Severidade da anomalia.
    /// </summary>
    public CopilotAnomalySeverity Severity { get; init; }

    /// <summary>
    /// Descrição técnica do diagnóstico em linguagem natural.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Ação sugerida pronta para execução em 1 clique.
    /// </summary>
    public CopilotRecommendationAction SuggestedAction { get; init; }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SearchCannibalizationAnomaly"/>.
    /// </summary>
    public SearchCannibalizationAnomaly(
        string searchTerm,
        string channelA,
        Guid campaignIdA,
        string campaignNameA,
        decimal cpcA,
        decimal cpaA,
        decimal spendA,
        string channelB,
        Guid campaignIdB,
        string campaignNameB,
        decimal cpcB,
        decimal cpaB,
        decimal spendB,
        decimal disparityRatio,
        decimal estimatedMonthlyWastedSpend,
        CopilotAnomalySeverity severity,
        string description,
        CopilotRecommendationAction suggestedAction)
    {
        SearchTerm = searchTerm;
        ChannelA = channelA;
        CampaignIdA = campaignIdA;
        CampaignNameA = campaignNameA;
        CpcA = cpcA;
        CpaA = cpaA;
        SpendA = spendA;
        ChannelB = channelB;
        CampaignIdB = campaignIdB;
        CampaignNameB = campaignNameB;
        CpcB = cpcB;
        CpaB = cpaB;
        SpendB = spendB;
        DisparityRatio = disparityRatio;
        EstimatedMonthlyWastedSpend = estimatedMonthlyWastedSpend;
        Severity = severity;
        Description = description;
        SuggestedAction = suggestedAction;
    }
}
