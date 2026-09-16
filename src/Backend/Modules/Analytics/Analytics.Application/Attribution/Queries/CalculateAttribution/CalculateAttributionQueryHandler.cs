using Analytics.Application.Attribution.Dtos;
using Analytics.Domain.Attribution;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Attribution.Queries.CalculateAttribution;

/// <summary>
/// Manipulador da consulta CQRS responsável pelo cálculo e comparação dos modelos de atribuição multi-canal.
/// </summary>
public sealed class CalculateAttributionQueryHandler : IQueryHandler<CalculateAttributionQuery, AttributionComparisonDto>
{
    private readonly IAttributionCalculator _calculator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CalculateAttributionQueryHandler"/>.
    /// </summary>
    /// <param name="calculator">Calculador de modelos de atribuição.</param>
    public CalculateAttributionQueryHandler(IAttributionCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    /// <inheritdoc />
    public Task<Result<AttributionComparisonDto>> Handle(CalculateAttributionQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Task.FromResult(Result<AttributionComparisonDto>.Failure(
                Error.Validation("Query.Null", "A consulta de atribuição não pode ser nula.")));
        }

        var domainJourneys = request.Journeys?
            .Select(j => new ConversionJourney(
                j.JourneyId,
                j.CustomerId,
                j.ConvertedAtUtc,
                j.ConversionValue,
                j.Touchpoints?
                    .Select(tp => new AttributionTouchpoint(
                        tp.Channel,
                        tp.CampaignName,
                        tp.OccurredAtUtc,
                        tp.TouchType,
                        tp.Cost))
                    .ToList() ?? new List<AttributionTouchpoint>()))
            .ToList() ?? new List<ConversionJourney>();

        var compareResult = _calculator.CompareModels(domainJourneys, request.ChannelCosts);
        if (compareResult.IsFailure)
        {
            return Task.FromResult(Result<AttributionComparisonDto>.Failure(compareResult.Error));
        }

        var domain = compareResult.Value;

        var firstTouchDto = MapChannels(domain.FirstTouchChannels);
        var lastTouchDto = MapChannels(domain.LastTouchChannels);
        var linearDto = MapChannels(domain.LinearChannels);

        var dto = new AttributionComparisonDto(
            firstTouchDto,
            lastTouchDto,
            linearDto,
            domain.TotalJourneys,
            domain.TotalConversions,
            domain.TotalConversionValue);

        return Task.FromResult(Result<AttributionComparisonDto>.Success(dto));
    }

    private static IReadOnlyList<ChannelAttributionDto> MapChannels(IReadOnlyList<ChannelAttributionResult> channels)
    {
        return channels.Select(c => new ChannelAttributionDto(
            c.Channel,
            c.AttributedConversions,
            c.AttributedRevenue,
            c.TotalTouchpoints,
            c.FirstTouchCount,
            c.LastTouchCount,
            c.AssistedConversionsCount,
            c.AttributedRoas,
            c.AttributedCpa)).ToList();
    }
}
