using SocialPublishing.Application.Posts;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Images;

/// <summary>
/// Monta brief de imagem a partir do Brand Voice e do post aprovado.
/// </summary>
public static class ImageBriefBuilder
{
    /// <summary>
    /// Proporção padrão de feed no Instagram.
    /// </summary>
    public const string InstagramFeedAspectRatio = "4:5";

    /// <summary>
    /// Proporção padrão de feed na Facebook Page.
    /// </summary>
    public const string FacebookFeedAspectRatio = "1:1";

    /// <summary>
    /// Constrói o brief para geração de imagem.
    /// </summary>
    /// <param name="brandVoice">Perfil de voz de marca do workspace.</param>
    /// <param name="post">Post orgânico aprovado.</param>
    /// <param name="imageSize">Resolução alvo (ex.: 1K).</param>
    /// <param name="optionalDirection">Direcionamento visual opcional.</param>
    /// <returns>Brief pronto para o adaptador de imagem.</returns>
    public static ImageBrief Build(
        BrandVoiceProfile brandVoice,
        OrganicPost post,
        string imageSize,
        string? optionalDirection)
    {
        ArgumentNullException.ThrowIfNull(brandVoice);
        ArgumentNullException.ThrowIfNull(post);

        var aspectRatio = post.Platform switch
        {
            OrganicPlatform.Instagram => InstagramFeedAspectRatio,
            OrganicPlatform.FacebookPage => FacebookFeedAspectRatio,
            _ => FacebookFeedAspectRatio,
        };

        var captionBody = OrganicCaptionBodyExtractor.GetBodyWithoutHashtags(post.Caption);

        return new ImageBrief(
            captionBody,
            post.Pillar,
            post.Platform,
            brandVoice.Persona,
            brandVoice.Tone,
            brandVoice.BusinessProfile?.BusinessSummary,
            brandVoice.BusinessProfile?.TargetAudience,
            brandVoice.BusinessProfile?.MainOffer,
            brandVoice.BusinessProfile?.Differentiators,
            brandVoice.VisualIdentity?.PrimaryColor,
            brandVoice.VisualIdentity?.SecondaryColor,
            brandVoice.VisualIdentity?.AccentColor,
            aspectRatio,
            imageSize,
            optionalDirection);
    }
}