using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain.Brand;

namespace SocialPublishing.Application.Brand;

/// <summary>
/// Mapeamento entre agregado e DTO de voz de marca.
/// </summary>
public static class BrandVoiceMapping
{
    /// <summary>
    /// Converte um perfil de domínio em DTO de API.
    /// </summary>
    /// <param name="profile">Agregado persistido.</param>
    /// <returns>DTO serializável.</returns>
    public static BrandVoiceDto ToDto(BrandVoiceProfile profile) =>
        new(
            profile.Id,
            profile.WorkspaceId,
            profile.Tone,
            profile.Persona,
            profile.MandatoryLinks.ToList(),
            profile.ForbiddenTerms.ToList(),
            profile.EmojiPolicy,
            profile.HashtagLimit,
            profile.PillarWeights.Authority,
            profile.PillarWeights.Sale,
            profile.PillarWeights.Connection,
            profile.PillarWeights.SocialProof,
            profile.LanguageCode,
            profile.CreatedAtUtc);
}
