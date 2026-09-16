namespace Analytics.Application.Taxonomy.Dtos;

/// <summary>
/// DTO de resposta para classificação taxonômica de campanhas, conjuntos de anúncios e criativos.
/// </summary>
/// <param name="ReferenceId">Identificador opcional de correlação.</param>
/// <param name="CampaignName">Nome da campanha analisada.</param>
/// <param name="AdSetName">Nome opcional do conjunto analisado.</param>
/// <param name="AdName">Nome opcional do anúncio analisado.</param>
/// <param name="FunnelStage">Estágio de funil inferido (Top, Middle, Bottom, Retention, Unclassified).</param>
/// <param name="AudienceType">Tipo de audiência identificado.</param>
/// <param name="CreativeType">Formato de criativo identificado.</param>
/// <param name="Tags">Lista de tags categóricas extraídas.</param>
public sealed record TaxonomyClassificationDto(
    string? ReferenceId,
    string CampaignName,
    string? AdSetName,
    string? AdName,
    string FunnelStage,
    string AudienceType,
    string CreativeType,
    IReadOnlyList<string> Tags);
