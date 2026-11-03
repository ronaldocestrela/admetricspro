using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Tetos de tamanho de legenda por rede orgânica (revisáveis quando a especificação da rede mudar).
/// </summary>
public static class CaptionLimits
{
    /// <summary>
    /// Teto de caracteres para Instagram.
    /// </summary>
    public const int Instagram = 2200;

    /// <summary>
    /// Teto de caracteres para Facebook Page.
    /// </summary>
    public const int FacebookPage = 2200;

    /// <summary>
    /// Teto de caracteres para TikTok.
    /// </summary>
    public const int TikTok = 2200;

    /// <summary>
    /// Obtém o teto de legenda para a plataforma informada.
    /// </summary>
    /// <param name="platform">Plataforma orgânica.</param>
    /// <returns>Número máximo de caracteres permitidos.</returns>
    public static int GetMaxLength(OrganicPlatform platform) => platform switch
    {
        OrganicPlatform.Instagram => Instagram,
        OrganicPlatform.FacebookPage => FacebookPage,
        OrganicPlatform.TikTok => TikTok,
        _ => Instagram,
    };
}
