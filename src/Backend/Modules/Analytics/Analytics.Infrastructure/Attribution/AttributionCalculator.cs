using Analytics.Domain.Attribution;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Infrastructure.Attribution;

/// <summary>
/// Implementação de alto desempenho do calculador e comparador de modelos de atribuição multi-canal,
/// cobrindo Primeiro Clique (First-Touch), Último Clique (Last-Touch), Linear e mapeamento de conversões assistidas.
/// </summary>
public sealed class AttributionCalculator : IAttributionCalculator
{
    /// <inheritdoc />
    public Result<AttributionComparisonResult> CompareModels(
        IEnumerable<ConversionJourney> journeys,
        IReadOnlyDictionary<string, decimal>? channelCosts = null)
    {
        var journeysList = journeys?.ToList() ?? new List<ConversionJourney>();

        var validationResult = ValidateJourneys(journeysList);
        if (validationResult.IsFailure)
        {
            return Result<AttributionComparisonResult>.Failure(validationResult.Error);
        }

        var firstTouchResult = CalculateModelInternal(journeysList, AttributionModelType.FirstTouch, channelCosts);
        var lastTouchResult = CalculateModelInternal(journeysList, AttributionModelType.LastTouch, channelCosts);
        var linearResult = CalculateModelInternal(journeysList, AttributionModelType.Linear, channelCosts);

        int totalJourneys = journeysList.Count;
        decimal totalConversions = journeysList.Count;
        decimal totalConversionValue = journeysList.Sum(j => j.ConversionValue);

        var comparison = new AttributionComparisonResult(
            firstTouchResult,
            lastTouchResult,
            linearResult,
            totalJourneys,
            totalConversions,
            totalConversionValue);

        return Result<AttributionComparisonResult>.Success(comparison);
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<ChannelAttributionResult>> CalculateModel(
        IEnumerable<ConversionJourney> journeys,
        AttributionModelType modelType,
        IReadOnlyDictionary<string, decimal>? channelCosts = null)
    {
        var journeysList = journeys?.ToList() ?? new List<ConversionJourney>();

        var validationResult = ValidateJourneys(journeysList);
        if (validationResult.IsFailure)
        {
            return Result<IReadOnlyList<ChannelAttributionResult>>.Failure(validationResult.Error);
        }

        var result = CalculateModelInternal(journeysList, modelType, channelCosts);
        return Result<IReadOnlyList<ChannelAttributionResult>>.Success(result);
    }

    private static Result ValidateJourneys(IReadOnlyList<ConversionJourney> journeys)
    {
        foreach (var journey in journeys)
        {
            if (journey.ConversionValue < 0)
            {
                return Result.Failure(Error.Validation(
                    "Attribution.InvalidConversionValue",
                    $"O valor de conversão da jornada '{journey.JourneyId}' não pode ser negativo."));
            }

            if (journey.Touchpoints != null)
            {
                foreach (var tp in journey.Touchpoints)
                {
                    if (tp.Cost < 0)
                    {
                        return Result.Failure(Error.Validation(
                            "Attribution.InvalidTouchpointCost",
                            "O custo do ponto de contato não pode ser negativo."));
                    }
                }
            }
        }

        return Result.Success();
    }

    private static IReadOnlyList<ChannelAttributionResult> CalculateModelInternal(
        IReadOnlyList<ConversionJourney> journeys,
        AttributionModelType modelType,
        IReadOnlyDictionary<string, decimal>? channelCosts)
    {
        var channelsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Estruturas de agregação
        var attributedConversions = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var attributedRevenue = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var totalTouchpoints = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var firstTouchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastTouchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var assistedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var journey in journeys)
        {
            if (journey.Touchpoints is null || journey.Touchpoints.Count == 0)
            {
                continue;
            }

            // Descarta touchpoints ocorridos após a conversão e ordena cronologicamente
            var validTouchpoints = journey.Touchpoints
                .Where(tp => tp.OccurredAtUtc <= journey.ConvertedAtUtc && !string.IsNullOrWhiteSpace(tp.Channel))
                .OrderBy(tp => tp.OccurredAtUtc)
                .ToList();

            if (validTouchpoints.Count == 0)
            {
                continue;
            }

            foreach (var tp in validTouchpoints)
            {
                channelsSet.Add(tp.Channel);
                totalTouchpoints[tp.Channel] = totalTouchpoints.GetValueOrDefault(tp.Channel) + 1;
            }

            var firstTp = validTouchpoints.First();
            var lastTp = validTouchpoints.Last();

            firstTouchCounts[firstTp.Channel] = firstTouchCounts.GetValueOrDefault(firstTp.Channel) + 1;
            lastTouchCounts[lastTp.Channel] = lastTouchCounts.GetValueOrDefault(lastTp.Channel) + 1;

            // Mapeamento de conversões assistidas: canais presentes na jornada que não foram o último toque
            var assistingChannels = validTouchpoints
                .Take(validTouchpoints.Count - 1)
                .Select(tp => tp.Channel)
                .Where(ch => !string.Equals(ch, lastTp.Channel, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var assistingChannel in assistingChannels)
            {
                assistedCounts[assistingChannel] = assistedCounts.GetValueOrDefault(assistingChannel) + 1;
            }

            // Aplicação do modelo de atribuição específico
            switch (modelType)
            {
                case AttributionModelType.FirstTouch:
                    attributedConversions[firstTp.Channel] = attributedConversions.GetValueOrDefault(firstTp.Channel) + 1.0m;
                    attributedRevenue[firstTp.Channel] = attributedRevenue.GetValueOrDefault(firstTp.Channel) + journey.ConversionValue;
                    break;

                case AttributionModelType.LastTouch:
                    attributedConversions[lastTp.Channel] = attributedConversions.GetValueOrDefault(lastTp.Channel) + 1.0m;
                    attributedRevenue[lastTp.Channel] = attributedRevenue.GetValueOrDefault(lastTp.Channel) + journey.ConversionValue;
                    break;

                case AttributionModelType.Linear:
                    decimal weight = 1.0m / validTouchpoints.Count;
                    decimal revenuePerTouch = journey.ConversionValue / validTouchpoints.Count;

                    foreach (var tp in validTouchpoints)
                    {
                        attributedConversions[tp.Channel] = attributedConversions.GetValueOrDefault(tp.Channel) + weight;
                        attributedRevenue[tp.Channel] = attributedRevenue.GetValueOrDefault(tp.Channel) + revenuePerTouch;
                    }
                    break;
            }
        }

        var results = new List<ChannelAttributionResult>(channelsSet.Count);

        foreach (var channel in channelsSet.OrderBy(c => c))
        {
            decimal convs = attributedConversions.GetValueOrDefault(channel);
            decimal rev = attributedRevenue.GetValueOrDefault(channel);
            int touches = totalTouchpoints.GetValueOrDefault(channel);
            int firsts = firstTouchCounts.GetValueOrDefault(channel);
            int lasts = lastTouchCounts.GetValueOrDefault(channel);
            int assisted = assistedCounts.GetValueOrDefault(channel);

            decimal? cost = null;
            if (channelCosts != null && channelCosts.TryGetValue(channel, out decimal directCost))
            {
                cost = directCost;
            }

            decimal? attributedRoas = null;
            if (cost.HasValue && cost.Value > 0)
            {
                attributedRoas = rev / cost.Value;
            }

            decimal? attributedCpa = null;
            if (cost.HasValue && cost.Value > 0 && convs > 0)
            {
                attributedCpa = cost.Value / convs;
            }

            results.Add(new ChannelAttributionResult(
                channel,
                convs,
                rev,
                touches,
                firsts,
                lasts,
                assisted,
                attributedRoas,
                attributedCpa));
        }

        return results;
    }
}
