using SocialPublishing.Application.Traction.DTOs;
using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Application.Traction;

/// <summary>
/// Mapeamento de entidades de sugestão de impulsionamento para DTOs.
/// </summary>
public static class PaidBoostSuggestionMapping
{
    /// <summary>
    /// Converte uma sugestão persistida em DTO de API.
    /// </summary>
    /// <param name="suggestion">Entidade de domínio.</param>
    /// <returns>DTO serializável.</returns>
    public static PaidBoostSuggestionDto ToDto(PaidBoostSuggestion suggestion) =>
        new(
            suggestion.Id,
            suggestion.OrganicPostId,
            suggestion.Platform,
            suggestion.Status,
            suggestion.Impressions,
            suggestion.EngagementRate,
            suggestion.EngagementMultiple,
            suggestion.DetectedAtUtc,
            suggestion.PaidDraftId);
}
