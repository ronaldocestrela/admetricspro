namespace Analytics.Domain.Taxonomy;

/// <summary>
/// Resultado da classificação taxonômica de uma campanha ou anúncio com enriquecimento de metadados estratégicos.
/// </summary>
public sealed record TaxonomyClassificationResult
{
    /// <summary>
    /// Identificador opcional de correlação de origem.
    /// </summary>
    public string? ReferenceId { get; init; }

    /// <summary>
    /// Nome original da campanha submetida à classificação.
    /// </summary>
    public string CampaignName { get; init; } = string.Empty;

    /// <summary>
    /// Nome original do conjunto de anúncios, se fornecido.
    /// </summary>
    public string? AdSetName { get; init; }

    /// <summary>
    /// Nome original do anúncio, se fornecido.
    /// </summary>
    public string? AdName { get; init; }

    /// <summary>
    /// Estágio de funil inferido.
    /// </summary>
    public FunnelStage FunnelStage { get; init; } = FunnelStage.Unclassified;

    /// <summary>
    /// Tipo de público-alvo detectado.
    /// </summary>
    public AudienceType AudienceType { get; init; } = AudienceType.Unknown;

    /// <summary>
    /// Formato de criativo detectado.
    /// </summary>
    public CreativeType CreativeType { get; init; } = CreativeType.Unknown;

    /// <summary>
    /// Coleção de tags descritivas extraídas da nomenclatura.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Detalhes estruturados dos marcadores taxonômicos identificados.
    /// </summary>
    public IReadOnlyList<TaxonomyTag> DetailedTags { get; init; } = Array.Empty<TaxonomyTag>();
}
