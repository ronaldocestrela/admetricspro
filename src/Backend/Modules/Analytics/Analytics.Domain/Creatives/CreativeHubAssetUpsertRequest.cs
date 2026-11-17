namespace Analytics.Domain.Creatives;

/// <summary>
/// Dados para upsert idempotente de ativo orgânico no Creative Hub pela fingerprint.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="MediaFingerprint">Hash estável da mídia.</param>
/// <param name="OrganicPostId">Post orgânico detectado.</param>
/// <param name="Network">Rede social.</param>
/// <param name="Permalink">URL pública, quando conhecida.</param>
/// <param name="Impressions">Impressões na detecção.</param>
/// <param name="EngagementRate">Taxa de engajamento.</param>
/// <param name="BaselineMedianEngagement">Mediana de referência.</param>
/// <param name="EngagementMultiple">Múltiplo sobre a mediana.</param>
/// <param name="DetectedAtUtc">Instante UTC da detecção.</param>
public sealed record CreativeHubAssetUpsertRequest(
    Guid WorkspaceId,
    string MediaFingerprint,
    Guid OrganicPostId,
    string Network,
    string? Permalink,
    long Impressions,
    decimal EngagementRate,
    decimal BaselineMedianEngagement,
    decimal EngagementMultiple,
    DateTime DetectedAtUtc);
