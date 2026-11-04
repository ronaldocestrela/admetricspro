using BuildingBlocks.Domain.Abstractions;

namespace Analytics.Domain.Creatives;

/// <summary>
/// Ativo de mídia registrado no Creative Hub, incluindo origem orgânica ou paga.
/// </summary>
public sealed class CreativeHubAsset : Entity<Guid>
{
    /// <summary>
    /// Inicializa um ativo do Creative Hub.
    /// </summary>
    /// <param name="id">Identificador do registro.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="mediaFingerprint">Hash estável da mídia.</param>
    /// <param name="organicPostId">Post orgânico de origem, quando aplicável.</param>
    /// <param name="network">Rede social de origem.</param>
    /// <param name="permalink">URL pública do post orgânico.</param>
    /// <param name="impressions">Impressões na detecção.</param>
    /// <param name="engagementRate">Taxa de engajamento na detecção.</param>
    /// <param name="baselineMedianEngagement">Mediana de engajamento usada.</param>
    /// <param name="engagementMultiple">Múltiplo sobre a mediana.</param>
    /// <param name="detectedAtUtc">Instante UTC da última detecção vinculada.</param>
    public CreativeHubAsset(
        Guid id,
        Guid workspaceId,
        string mediaFingerprint,
        Guid? organicPostId,
        string network,
        string? permalink,
        long impressions,
        decimal engagementRate,
        decimal baselineMedianEngagement,
        decimal engagementMultiple,
        DateTime detectedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        MediaFingerprint = mediaFingerprint;
        OrganicPostId = organicPostId;
        Network = network;
        Permalink = permalink;
        Impressions = impressions;
        EngagementRate = engagementRate;
        BaselineMedianEngagement = baselineMedianEngagement;
        EngagementMultiple = engagementMultiple;
        DetectedAtUtc = detectedAtUtc;
    }

    private CreativeHubAsset()
        : base(Guid.Empty)
    {
        MediaFingerprint = string.Empty;
        Network = string.Empty;
    }

    /// <summary>
    /// Obtém o workspace proprietário.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o fingerprint estável da mídia.
    /// </summary>
    public string MediaFingerprint { get; private set; }

    /// <summary>
    /// Obtém o post orgânico associado, quando a origem é orgânica.
    /// </summary>
    public Guid? OrganicPostId { get; private set; }

    /// <summary>
    /// Obtém a rede de origem.
    /// </summary>
    public string Network { get; private set; }

    /// <summary>
    /// Obtém o permalink do post orgânico, quando disponível.
    /// </summary>
    public string? Permalink { get; private set; }

    /// <summary>
    /// Obtém impressões registradas na última detecção.
    /// </summary>
    public long Impressions { get; private set; }

    /// <summary>
    /// Obtém a taxa de engajamento registrada.
    /// </summary>
    public decimal EngagementRate { get; private set; }

    /// <summary>
    /// Obtém a mediana de engajamento de referência.
    /// </summary>
    public decimal BaselineMedianEngagement { get; private set; }

    /// <summary>
    /// Obtém o múltiplo de engajamento.
    /// </summary>
    public decimal EngagementMultiple { get; private set; }

    /// <summary>
    /// Obtém o instante UTC da última atualização por tração orgânica.
    /// </summary>
    public DateTime DetectedAtUtc { get; private set; }

    /// <summary>
    /// Atualiza métricas e metadados orgânicos de um ativo existente.
    /// </summary>
    /// <param name="organicPostId">Post orgânico.</param>
    /// <param name="network">Rede social.</param>
    /// <param name="permalink">Permalink, se conhecido.</param>
    /// <param name="impressions">Impressões.</param>
    /// <param name="engagementRate">Taxa de engajamento.</param>
    /// <param name="baselineMedianEngagement">Mediana.</param>
    /// <param name="engagementMultiple">Múltiplo.</param>
    /// <param name="detectedAtUtc">Instante da detecção.</param>
    public void UpdateOrganicTraction(
        Guid organicPostId,
        string network,
        string? permalink,
        long impressions,
        decimal engagementRate,
        decimal baselineMedianEngagement,
        decimal engagementMultiple,
        DateTime detectedAtUtc)
    {
        OrganicPostId = organicPostId;
        Network = network;
        Permalink = permalink;
        Impressions = impressions;
        EngagementRate = engagementRate;
        BaselineMedianEngagement = baselineMedianEngagement;
        EngagementMultiple = engagementMultiple;
        DetectedAtUtc = detectedAtUtc;
    }
}
