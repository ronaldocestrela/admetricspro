using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Application.Traction.DTOs;

/// <summary>
/// Projeção HTTP de uma sugestão de impulsionamento por tração orgânica.
/// </summary>
/// <param name="Id">Identificador da sugestão.</param>
/// <param name="OrganicPostId">Post orgânico associado.</param>
/// <param name="Platform">Rede social do post.</param>
/// <param name="Status">Status atual da sugestão.</param>
/// <param name="Impressions">Impressões no momento da detecção.</param>
/// <param name="EngagementRate">Taxa de engajamento observada.</param>
/// <param name="EngagementMultiple">Múltiplo sobre a mediana da janela.</param>
/// <param name="DetectedAtUtc">Instante UTC da detecção.</param>
/// <param name="PaidDraftId">Identificador do rascunho pago, quando aceito.</param>
public sealed record PaidBoostSuggestionDto(
    Guid Id,
    Guid OrganicPostId,
    OrganicPlatform Platform,
    PaidBoostSuggestionStatus Status,
    long Impressions,
    decimal EngagementRate,
    decimal EngagementMultiple,
    DateTime DetectedAtUtc,
    Guid? PaidDraftId);
