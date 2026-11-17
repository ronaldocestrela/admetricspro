using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Copy;

/// <summary>
/// Instruções fixas de formato por rede e tipo de publicação.
/// </summary>
public static class CopyFormatInstructions
{
    /// <summary>
    /// Obtém a instrução de formato para o par rede/formato informado.
    /// </summary>
    /// <param name="platform">Plataforma orgânica.</param>
    /// <param name="format">Formato de publicação.</param>
    /// <returns>Texto de instrução para o prompt.</returns>
    public static string Get(OrganicPlatform platform, OrganicPostFormat format) => (platform, format) switch
    {
        (_, OrganicPostFormat.FeedCarousel) =>
            "Estruture a legenda como sequência de cards numerados (Card 1, Card 2, …), uma ideia por card, coerente com carrossel de feed.",

        (_, OrganicPostFormat.Reel) or (_, OrganicPostFormat.TikTokVideo) =>
            "Abra com um gancho falado curto na primeira linha (máximo 12 palavras), depois desenvolva a legenda.",

        (OrganicPlatform.Instagram, OrganicPostFormat.FeedImage) =>
            "Tom visual para feed do Instagram: primeira linha forte, quebras de linha e CTA discreto no final.",

        (OrganicPlatform.FacebookPage, OrganicPostFormat.FeedImage) =>
            "Tom para publicação de Facebook Page: parágrafos curtos, contexto claro para leitura no feed da página e CTA explícito.",

        (OrganicPlatform.TikTok, _) =>
            "Tom conversacional para TikTok: direto, informal e alinhado ao gancho do vídeo.",

        _ =>
            "Legenda equilibrada para feed, com gancho inicial e fechamento alinhado ao pilar editorial.",
    };
}
