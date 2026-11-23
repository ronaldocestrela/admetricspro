using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace WebApp.Components.Social;

/// <summary>
/// Rótulos legíveis de plataforma e formato para superfícies de posts orgânicos.
/// </summary>
public static class OrganicPostDisplayLabels
{
    /// <summary>
    /// Rótulo legível da rede de destino.
    /// </summary>
    /// <param name="platform">Plataforma do post.</param>
    /// <returns>Texto para UI.</returns>
    public static string FormatPlatform(OrganicPlatform platform) => platform switch
    {
        OrganicPlatform.Instagram => "Instagram",
        OrganicPlatform.FacebookPage => "Facebook Page",
        OrganicPlatform.TikTok => "TikTok",
        _ => platform.ToString(),
    };

    /// <summary>
    /// Rótulo legível do formato de publicação.
    /// </summary>
    /// <param name="format">Formato do post.</param>
    /// <returns>Texto para UI.</returns>
    public static string FormatPostFormat(OrganicPostFormat format) => format switch
    {
        OrganicPostFormat.FeedImage => "Imagem no feed",
        OrganicPostFormat.FeedCarousel => "Carrossel no feed",
        OrganicPostFormat.Reel => "Reel",
        OrganicPostFormat.TikTokVideo => "Vídeo TikTok",
        _ => format.ToString(),
    };
}