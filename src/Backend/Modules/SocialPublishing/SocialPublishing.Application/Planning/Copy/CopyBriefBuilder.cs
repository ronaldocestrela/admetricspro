using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Copy;

/// <summary>
/// Monta o brief de cópia a partir do Brand Voice e do slot editorial.
/// </summary>
public static class CopyBriefBuilder
{
    /// <summary>
    /// Constrói o brief determinístico para o provedor de IA.
    /// </summary>
    /// <param name="brandVoice">Perfil de voz de marca do workspace.</param>
    /// <param name="slot">Dados do slot editorial.</param>
    /// <returns>Brief pronto para geração.</returns>
    public static CopyBrief Build(BrandVoiceProfile brandVoice, EditorialSlotCopyInput slot)
    {
        ArgumentNullException.ThrowIfNull(brandVoice);
        ArgumentNullException.ThrowIfNull(slot);

        var formatInstruction = CopyFormatInstructions.Get(slot.Platform, slot.Format);
        var maxCaption = CaptionLimits.GetMaxLength(slot.Platform);

        return new CopyBrief(
            brandVoice.Persona,
            brandVoice.Tone,
            brandVoice.ForbiddenTerms.ToList(),
            brandVoice.MandatoryLinks.ToList(),
            brandVoice.EmojiPolicy,
            brandVoice.LanguageCode,
            brandVoice.HashtagLimit,
            maxCaption,
            slot.Pillar,
            slot.Platform,
            slot.Format,
            formatInstruction);
    }
}
