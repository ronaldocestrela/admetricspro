namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Tipos MIME permitidos para mídia orgânica.
/// </summary>
public static class OrganicMediaMimeTypes
{
    /// <summary>
    /// JPEG permitido para imagens de feed e carrossel.
    /// </summary>
    public const string Jpeg = "image/jpeg";

    /// <summary>
    /// PNG permitido para imagens de feed e carrossel.
    /// </summary>
    public const string Png = "image/png";

    /// <summary>
    /// MP4 permitido para Reels e TikTok.
    /// </summary>
    public const string Mp4 = "video/mp4";

    /// <summary>
    /// Verifica se o MIME está na lista permitida.
    /// </summary>
    /// <param name="mimeType">Tipo informado.</param>
    /// <returns><c>true</c> quando permitido.</returns>
    public static bool IsAllowed(string mimeType) =>
        mimeType is Jpeg or Png or Mp4;

    /// <summary>
    /// Indica se o MIME representa imagem estática.
    /// </summary>
    /// <param name="mimeType">Tipo informado.</param>
    /// <returns><c>true</c> para JPEG ou PNG.</returns>
    public static bool IsImage(string mimeType) => mimeType is Jpeg or Png;

    /// <summary>
    /// Indica se o MIME representa vídeo MP4.
    /// </summary>
    /// <param name="mimeType">Tipo informado.</param>
    /// <returns><c>true</c> para MP4.</returns>
    public static bool IsVideo(string mimeType) => mimeType == Mp4;
}
