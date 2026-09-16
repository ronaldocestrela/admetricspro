using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Taxonomy;

/// <summary>
/// Contrato do motor automatizado de classificação taxonômica para identificação de funil, públicos e formatos.
/// </summary>
public interface ITaxonomyClassifier
{
    /// <summary>
    /// Classifica uma campanha ou anúncio a partir de suas nomenclaturas.
    /// </summary>
    /// <param name="campaignName">Nome da campanha.</param>
    /// <param name="adSetName">Nome opcional do conjunto de anúncios.</param>
    /// <param name="adName">Nome opcional do anúncio.</param>
    /// <param name="referenceId">Identificador opcional de correlação.</param>
    /// <returns>Resultado da classificação taxonômica enriquecida.</returns>
    Result<TaxonomyClassificationResult> Classify(
        string campaignName,
        string? adSetName = null,
        string? adName = null,
        string? referenceId = null);

    /// <summary>
    /// Classifica em lote uma coleção de campanhas e anúncios.
    /// </summary>
    /// <param name="items">Coleção de itens com nomenclaturas a classificar.</param>
    /// <returns>Resultado contendo a lista de classificações realizadas.</returns>
    Result<IReadOnlyList<TaxonomyClassificationResult>> ClassifyBatch(
        IEnumerable<CampaignTaxonomyInput> items);
}
