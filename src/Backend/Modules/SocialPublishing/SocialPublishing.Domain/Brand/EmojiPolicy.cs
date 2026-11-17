namespace SocialPublishing.Domain.Brand;

/// <summary>
/// Política de uso de emojis no DNA de marca.
/// </summary>
public enum EmojiPolicy
{
    /// <summary>
    /// Emojis permitidos livremente.
    /// </summary>
    Allowed = 1,

    /// <summary>
    /// Uso moderado de emojis.
    /// </summary>
    Sparingly = 2,

    /// <summary>
    /// Emojis proibidos.
    /// </summary>
    Forbidden = 3,
}
