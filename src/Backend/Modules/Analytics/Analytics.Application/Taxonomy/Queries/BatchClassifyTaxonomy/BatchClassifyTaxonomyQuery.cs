using Analytics.Application.Taxonomy.Dtos;
using Analytics.Domain.Taxonomy;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Taxonomy.Queries.BatchClassifyTaxonomy;

/// <summary>
/// Entrada para item de classificação taxonômica em lote.
/// </summary>
/// <param name="CampaignName">Nome da campanha.</param>
/// <param name="AdSetName">Nome opcional do conjunto.</param>
/// <param name="AdName">Nome opcional do anúncio.</param>
/// <param name="ReferenceId">Identificador opcional de correlação.</param>
public sealed record BatchClassifyTaxonomyItem(
    string CampaignName,
    string? AdSetName = null,
    string? AdName = null,
    string? ReferenceId = null);

/// <summary>
/// Consulta para classificação taxonômica em lote de múltiplas campanhas e criativos.
/// </summary>
/// <param name="Items">Lista de itens a classificar.</param>
public sealed record BatchClassifyTaxonomyQuery(
    IReadOnlyList<BatchClassifyTaxonomyItem> Items) : IQuery<IReadOnlyList<TaxonomyClassificationDto>>;

/// <summary>
/// Manipulador da consulta <see cref="BatchClassifyTaxonomyQuery"/>.
/// </summary>
public sealed class BatchClassifyTaxonomyQueryHandler : IQueryHandler<BatchClassifyTaxonomyQuery, IReadOnlyList<TaxonomyClassificationDto>>
{
    private readonly ITaxonomyClassifier _classifier;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BatchClassifyTaxonomyQueryHandler"/>.
    /// </summary>
    /// <param name="classifier">Motor de taxonomia.</param>
    public BatchClassifyTaxonomyQueryHandler(ITaxonomyClassifier classifier)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TaxonomyClassificationDto>>> Handle(
        BatchClassifyTaxonomyQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Items is null || query.Items.Count == 0)
        {
            return Task.FromResult(Result<IReadOnlyList<TaxonomyClassificationDto>>.Success(Array.Empty<TaxonomyClassificationDto>()));
        }

        var inputs = query.Items.Select(i => new CampaignTaxonomyInput(i.CampaignName, i.AdSetName, i.AdName, i.ReferenceId));
        var batchResult = _classifier.ClassifyBatch(inputs);

        if (batchResult.IsFailure)
        {
            return Task.FromResult(Result<IReadOnlyList<TaxonomyClassificationDto>>.Failure(batchResult.Error));
        }

        var dtos = batchResult.Value.Select(val => new TaxonomyClassificationDto(
            ReferenceId: val.ReferenceId,
            CampaignName: val.CampaignName,
            AdSetName: val.AdSetName,
            AdName: val.AdName,
            FunnelStage: val.FunnelStage.ToString(),
            AudienceType: val.AudienceType.ToString(),
            CreativeType: val.CreativeType.ToString(),
            Tags: val.Tags)).ToList();

        return Task.FromResult(Result<IReadOnlyList<TaxonomyClassificationDto>>.Success(dtos.AsReadOnly()));
    }
}
