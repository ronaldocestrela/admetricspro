namespace Analytics.Domain.Taxonomy;

/// <summary>
/// Estrutura de entrada para classificação taxonômica de campanhas, conjuntos e anúncios.
/// </summary>
/// <param name="CampaignName">Nome descritivo da campanha.</param>
/// <param name="AdSetName">Nome opcional do conjunto ou grupo de anúncios.</param>
/// <param name="AdName">Nome opcional do anúncio ou criativo.</param>
/// <param name="ReferenceId">Identificador opcional de correlação (ex: Id da campanha ou métrica).</param>
public sealed record CampaignTaxonomyInput(
    string CampaignName,
    string? AdSetName = null,
    string? AdName = null,
    string? ReferenceId = null);
