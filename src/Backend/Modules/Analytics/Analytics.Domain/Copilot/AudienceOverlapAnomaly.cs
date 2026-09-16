namespace Analytics.Domain.Copilot;

/// <summary>
/// Anomalia analítica que representa sobreposição prejudicial de públicos entre conjuntos de anúncios no Meta Ads.
/// </summary>
public sealed record AudienceOverlapAnomaly
{
    /// <summary>
    /// Identificador único do primeiro conjunto analisado.
    /// </summary>
    public Guid AdSetIdA { get; init; }

    /// <summary>
    /// Nome do primeiro conjunto analisado.
    /// </summary>
    public string AdSetNameA { get; init; }

    /// <summary>
    /// Identificador da campanha do primeiro conjunto.
    /// </summary>
    public Guid CampaignIdA { get; init; }

    /// <summary>
    /// Nome da campanha do primeiro conjunto.
    /// </summary>
    public string CampaignNameA { get; init; }

    /// <summary>
    /// Identificador único do segundo conjunto analisado.
    /// </summary>
    public Guid AdSetIdB { get; init; }

    /// <summary>
    /// Nome do segundo conjunto analisado.
    /// </summary>
    public string AdSetNameB { get; init; }

    /// <summary>
    /// Identificador da campanha do segundo conjunto.
    /// </summary>
    public Guid CampaignIdB { get; init; }

    /// <summary>
    /// Nome da campanha do segundo conjunto.
    /// </summary>
    public string CampaignNameB { get; init; }

    /// <summary>
    /// Percentual de sobreposição calculado entre as segmentações (0 a 100%).
    /// </summary>
    public decimal OverlapPercentage { get; init; }

    /// <summary>
    /// CPM do primeiro conjunto.
    /// </summary>
    public decimal CpmA { get; init; }

    /// <summary>
    /// CPM do segundo conjunto.
    /// </summary>
    public decimal CpmB { get; init; }

    /// <summary>
    /// Nível de severidade da anomalia.
    /// </summary>
    public CopilotAnomalySeverity Severity { get; init; }

    /// <summary>
    /// Diagnóstico explicativo em linguagem natural descrevendo o impacto da sobreposição.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Ação sugerida pronta para execução em 1 clique.
    /// </summary>
    public CopilotRecommendationAction SuggestedAction { get; init; }

    /// <summary>
    /// Tags ou termos comuns causadores da sobreposição.
    /// </summary>
    public IReadOnlyList<string> SharedTargetingTags { get; init; }

    /// <summary>
    /// Cria uma nova instância de <see cref="AudienceOverlapAnomaly"/>.
    /// </summary>
    public AudienceOverlapAnomaly(
        Guid adSetIdA,
        string adSetNameA,
        Guid campaignIdA,
        string campaignNameA,
        Guid adSetIdB,
        string adSetNameB,
        Guid campaignIdB,
        string campaignNameB,
        decimal overlapPercentage,
        decimal cpmA,
        decimal cpmB,
        CopilotAnomalySeverity severity,
        string description,
        CopilotRecommendationAction suggestedAction,
        IEnumerable<string>? sharedTargetingTags = null)
    {
        AdSetIdA = adSetIdA;
        AdSetNameA = adSetNameA;
        CampaignIdA = campaignIdA;
        CampaignNameA = campaignNameA;
        AdSetIdB = adSetIdB;
        AdSetNameB = adSetNameB;
        CampaignIdB = campaignIdB;
        CampaignNameB = campaignNameB;
        OverlapPercentage = overlapPercentage;
        CpmA = cpmA;
        CpmB = cpmB;
        Severity = severity;
        Description = description;
        SuggestedAction = suggestedAction;
        SharedTargetingTags = sharedTargetingTags?.ToList() ?? new List<string>();
    }
}
