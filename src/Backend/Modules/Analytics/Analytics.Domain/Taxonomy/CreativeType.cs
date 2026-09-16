namespace Analytics.Domain.Taxonomy;

/// <summary>
/// Define o formato visual ou tipo de criativo de anúncio inferido pela taxonomia.
/// </summary>
public enum CreativeType
{
    /// <summary>
    /// Formato em vídeo (Reels, Shorts, TikTok, YouTube, Feed Video).
    /// </summary>
    Video = 1,

    /// <summary>
    /// Imagem estática / banner gráfico (Feed, Display, Stories).
    /// </summary>
    Image = 2,

    /// <summary>
    /// Formato carrossel multiproduto / multimodelo.
    /// </summary>
    Carousel = 3,

    /// <summary>
    /// Anúncio em formato texto para redes de pesquisa (Google/Bing Search).
    /// </summary>
    SearchText = 4,

    /// <summary>
    /// Formato dinâmico ou catálogo automatizado (Advantage+, PMax, DPA).
    /// </summary>
    Dynamic = 5,

    /// <summary>
    /// Não identificado ou genérico.
    /// </summary>
    Unknown = 99
}
