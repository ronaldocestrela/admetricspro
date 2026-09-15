namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Tipos de formato de criativo publicitário suportados entre as redes.
/// </summary>
public enum AdCreativeType
{
    /// <summary>
    /// Formato não especificado ou desconhecido.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Imagem estática única.
    /// </summary>
    Image = 1,

    /// <summary>
    /// Vídeo (feed, reels, tiktok, shorts).
    /// </summary>
    Video = 2,

    /// <summary>
    /// Carrossel com múltiplos cartões / imagens / vídeos.
    /// </summary>
    Carousel = 3,

    /// <summary>
    /// Anúncio em formato texto tradicional (ex: Search básico).
    /// </summary>
    Text = 4,

    /// <summary>
    /// Anúncio Responsivo de Pesquisa (Google RSA / Bing RSA) com múltiplos títulos e descrições.
    /// </summary>
    ResponsiveSearch = 5,

    /// <summary>
    /// Criativo dinâmico multivariado.
    /// </summary>
    Dynamic = 6
}
