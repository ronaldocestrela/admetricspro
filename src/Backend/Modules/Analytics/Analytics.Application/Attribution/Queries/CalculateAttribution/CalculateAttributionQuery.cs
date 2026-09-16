using Analytics.Application.Attribution.Dtos;
using Analytics.Domain.Attribution;
using BuildingBlocks.Application.Messaging;

namespace Analytics.Application.Attribution.Queries.CalculateAttribution;

/// <summary>
/// Consulta CQRS para processar jornadas de conversão e comparar modelos de atribuição (First-Touch, Last-Touch e Linear).
/// </summary>
/// <param name="Journeys">Lista de jornadas de conversão contendo seus respectivos pontos de contato.</param>
/// <param name="ModelType">Modelo específico opcional a ser calculado exclusivamente (se nulo, compara os 3 modelos side-by-side).</param>
/// <param name="ChannelCosts">Dicionário opcional contendo o custo total por canal para cálculo de ROAS e CPA atribuídos.</param>
public sealed record CalculateAttributionQuery(
    IReadOnlyList<ConversionJourneyInput> Journeys,
    AttributionModelType? ModelType = null,
    IReadOnlyDictionary<string, decimal>? ChannelCosts = null) : IQuery<AttributionComparisonDto>;
