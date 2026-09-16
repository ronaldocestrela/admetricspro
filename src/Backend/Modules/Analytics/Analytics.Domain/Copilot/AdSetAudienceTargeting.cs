namespace Analytics.Domain.Copilot;

/// <summary>
/// Modelo de entrada representando a segmentação de público e métricas de um conjunto de anúncios para análise de overlap.
/// </summary>
public sealed record AdSetAudienceTargeting
{
    /// <summary>
    /// Identificador único do conjunto de anúncios.
    /// </summary>
    public Guid AdSetId { get; init; }

    /// <summary>
    /// Nome do conjunto de anúncios.
    /// </summary>
    public string AdSetName { get; init; } = string.Empty;

    /// <summary>
    /// Identificador único da campanha à qual o conjunto pertence.
    /// </summary>
    public Guid CampaignId { get; init; }

    /// <summary>
    /// Nome da campanha.
    /// </summary>
    public string CampaignName { get; init; } = string.Empty;

    /// <summary>
    /// Status operacional do conjunto (ex.: Active, Paused).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gasto financeiro acumulado no período de análise.
    /// </summary>
    public decimal Spend { get; init; }

    /// <summary>
    /// Total de impressões registradas.
    /// </summary>
    public long Impressions { get; init; }

    /// <summary>
    /// Custo por Mil Impressões (CPM).
    /// </summary>
    public decimal Cpm { get; init; }

    /// <summary>
    /// Custo por Aquisição / Conversão (CPA).
    /// </summary>
    public decimal Cpa { get; init; }

    /// <summary>
    /// Lista normalizada de tags de segmentação (interesses, lookalikes, demografia, geolocalização).
    /// </summary>
    public IReadOnlyList<string> TargetingTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AdSetAudienceTargeting"/>.
    /// </summary>
    public AdSetAudienceTargeting(
        Guid adSetId,
        string adSetName,
        Guid campaignId,
        string campaignName,
        string status,
        decimal spend,
        long impressions,
        decimal cpm,
        decimal cpa,
        IEnumerable<string>? targetingTags = null)
    {
        AdSetId = adSetId;
        AdSetName = adSetName;
        CampaignId = campaignId;
        CampaignName = campaignName;
        Status = status;
        Spend = spend;
        Impressions = impressions;
        Cpm = cpm;
        Cpa = cpa;
        TargetingTags = targetingTags?.Select(t => t.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList() ?? new List<string>();
    }
}
