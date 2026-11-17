namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Formato de publicação orgânica suportado.
/// </summary>
public enum OrganicPostFormat
{
    /// <summary>
    /// Imagem única no feed.
    /// </summary>
    FeedImage = 1,

    /// <summary>
    /// Carrossel de imagens no feed.
    /// </summary>
    FeedCarousel = 2,

    /// <summary>
    /// Reels no Instagram.
    /// </summary>
    Reel = 3,

    /// <summary>
    /// Vídeo vertical no TikTok.
    /// </summary>
    TikTokVideo = 4,
}
