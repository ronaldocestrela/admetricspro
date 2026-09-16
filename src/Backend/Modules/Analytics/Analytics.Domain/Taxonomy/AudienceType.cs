namespace Analytics.Domain.Taxonomy;

/// <summary>
/// Define a tipologia de público-alvo detectada na segmentação da campanha ou conjunto de anúncios.
/// </summary>
public enum AudienceType
{
    /// <summary>
    /// Público aberto / amplo / sem segmentação restritiva (Broad).
    /// </summary>
    Broad = 1,

    /// <summary>
    /// Público semelhante / Lookalike / LAL (1%, 2%, etc.).
    /// </summary>
    Lookalike = 2,

    /// <summary>
    /// Público segmentado por interesses, comportamentos ou afinidades.
    /// </summary>
    Interest = 3,

    /// <summary>
    /// Público personalizado / Custom Audience (Lista de clientes, visitantes do site, engajamento).
    /// </summary>
    CustomAudience = 4,

    /// <summary>
    /// Campanhas de busca institucional / termos com o nome da marca (Brand).
    /// </summary>
    SearchBrand = 5,

    /// <summary>
    /// Campanhas de busca genérica / termos sem referência à marca (Non-Brand).
    /// </summary>
    SearchNonBrand = 6,

    /// <summary>
    /// Não identificado ou não aplicável.
    /// </summary>
    Unknown = 99
}
