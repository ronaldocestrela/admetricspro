using Analytics.Application.Taxonomy.Dtos;
using Analytics.Domain.Taxonomy;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Taxonomy.Queries.ClassifyTaxonomy;

/// <summary>
/// Consulta para classificação taxonômica individual de uma campanha, conjunto ou anúncio.
/// </summary>
/// <param name="CampaignName">Nome da campanha.</param>
/// <param name="AdSetName">Nome opcional do conjunto.</param>
/// <param name="AdName">Nome opcional do anúncio.</param>
/// <param name="ReferenceId">Identificador opcional de correlação.</param>
public sealed record ClassifyTaxonomyQuery(
    string CampaignName,
    string? AdSetName = null,
    string? AdName = null,
    string? ReferenceId = null) : IQuery<TaxonomyClassificationDto>;

/// <summary>
/// Manipulador da consulta <see cref="ClassifyTaxonomyQuery"/>.
/// </summary>
public sealed class ClassifyTaxonomyQueryHandler : IQueryHandler<ClassifyTaxonomyQuery, TaxonomyClassificationDto>
{
    private readonly ITaxonomyClassifier _classifier;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ClassifyTaxonomyQueryHandler"/>.
    /// </summary>
    /// <param name="classifier">Motor de taxonomia.</param>
    public ClassifyTaxonomyQueryHandler(ITaxonomyClassifier classifier)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    /// <inheritdoc />
    public Task<Result<TaxonomyClassificationDto>> Handle(
        ClassifyTaxonomyQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = _classifier.Classify(query.CampaignName, query.AdSetName, query.AdName, query.ReferenceId);
        if (result.IsFailure)
        {
            return Task.FromResult(Result<TaxonomyClassificationDto>.Failure(result.Error));
        }

        var val = result.Value;
        var dto = new TaxonomyClassificationDto(
            ReferenceId: val.ReferenceId,
            CampaignName: val.CampaignName,
            AdSetName: val.AdSetName,
            AdName: val.AdName,
            FunnelStage: val.FunnelStage.ToString(),
            AudienceType: val.AudienceType.ToString(),
            CreativeType: val.CreativeType.ToString(),
            Tags: val.Tags);

        return Task.FromResult(Result<TaxonomyClassificationDto>.Success(dto));
    }
}
