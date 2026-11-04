using MediatR;

namespace SocialPublishing.Contracts.Notifications;

/// <summary>
/// Notificação in-memory emitida após persistir uma sugestão de impulsionamento por tração orgânica.
/// </summary>
/// <param name="TenantId">Identificador do tenant.</param>
/// <param name="WorkspaceId">Workspace proprietário do post.</param>
/// <param name="OrganicPostId">Post orgânico com tração detectada.</param>
/// <param name="Network">Rede social (nome canônico, ex.: Instagram).</param>
/// <param name="MediaFingerprint">Hash SHA-256 da mídia primária; nulo quando indisponível.</param>
/// <param name="Permalink">URL pública do post na rede; nulo quando ainda não resolvida.</param>
/// <param name="Impressions">Impressões no momento da detecção.</param>
/// <param name="EngagementRate">Taxa de engajamento calculada.</param>
/// <param name="BaselineMedianEngagement">Mediana de engajamento na janela.</param>
/// <param name="EngagementMultiple">Múltiplo em relação à mediana.</param>
/// <param name="DetectedAtUtc">Instante UTC da detecção.</param>
public sealed record OrganicTractionDetectedNotification(
    Guid TenantId,
    Guid WorkspaceId,
    Guid OrganicPostId,
    string Network,
    string? MediaFingerprint,
    string? Permalink,
    long Impressions,
    decimal EngagementRate,
    decimal BaselineMedianEngagement,
    decimal EngagementMultiple,
    DateTime DetectedAtUtc) : INotification;
