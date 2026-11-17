using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Serviço de domínio puro que detecta posts orgânicos com engajamento anômalo em relação à mediana do workspace na mesma rede.
/// </summary>
public static class OrganicTractionDetector
{
    /// <summary>
    /// Avalia a série informada e retorna hits de tração ou falha quando não há dados suficientes em nenhuma rede.
    /// </summary>
    /// <param name="workspaceId">Workspace em análise.</param>
    /// <param name="series">Amostras de métricas por post publicado.</param>
    /// <param name="asOfUtc">Instante de referência em UTC para a janela retroativa.</param>
    /// <returns>Relatório com hits e posts ignorados, ou <see cref="SocialErrors.OrganicTractionInsufficientData"/>.</returns>
    public static Result<OrganicTractionReport> Detect(
        Guid workspaceId,
        IReadOnlyList<OrganicPostInsightSample> series,
        DateTime asOfUtc)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<OrganicTractionReport>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (asOfUtc.Kind != DateTimeKind.Utc)
        {
            return Result<OrganicTractionReport>.Failure(SocialErrors.ScheduleInstantNotUtc);
        }

        var windowStart = asOfUtc.AddDays(-OrganicTractionThresholds.BaselineWindowDays);
        var hits = new List<OrganicTractionHit>();
        var skipped = new List<OrganicTractionSkippedPost>();
        var anyPlatformWithBaseline = false;

        foreach (var platformGroup in series
                     .Where(s => s.WorkspaceId == workspaceId && s.Impressions > 0)
                     .Where(s => s.PublishedAtUtc >= windowStart && s.PublishedAtUtc <= asOfUtc)
                     .GroupBy(s => s.Platform))
        {
            var inWindow = platformGroup.ToList();
            if (inWindow.Count < OrganicTractionThresholds.MinimumSampleSize)
            {
                continue;
            }

            anyPlatformWithBaseline = true;
            var engagementRates = inWindow
                .Select(s => ComputeEngagementRate(s))
                .OrderBy(r => r)
                .ToList();
            var median = ComputeMedian(engagementRates);
            var threshold = median * OrganicTractionThresholds.EngagementMultiple;

            foreach (var sample in inWindow)
            {
                var rate = ComputeEngagementRate(sample);
                if (sample.Impressions < OrganicTractionThresholds.MinimumImpressions ||
                    rate < threshold)
                {
                    continue;
                }

                if (sample.HasOpenSuggestion)
                {
                    skipped.Add(new OrganicTractionSkippedPost(
                        sample.OrganicPostId,
                        SocialErrors.BoostAlreadyRequested));
                    continue;
                }

                var multiple = median == 0m ? 0m : rate / median;
                hits.Add(new OrganicTractionHit(
                    sample.OrganicPostId,
                    sample.Platform,
                    sample.Impressions,
                    rate,
                    median,
                    multiple));
            }
        }

        if (!anyPlatformWithBaseline)
        {
            return Result<OrganicTractionReport>.Failure(SocialErrors.OrganicTractionInsufficientData);
        }

        return Result<OrganicTractionReport>.Success(new OrganicTractionReport(hits, skipped));
    }

    private static decimal ComputeEngagementRate(OrganicPostInsightSample sample)
    {
        var engaged = sample.Likes + sample.Comments + sample.Shares + sample.Saves;
        return (decimal)engaged / sample.Impressions;
    }

    private static decimal ComputeMedian(IReadOnlyList<decimal> sortedRates)
    {
        var count = sortedRates.Count;
        if (count == 0)
        {
            return 0m;
        }

        if (count % 2 == 1)
        {
            return sortedRates[count / 2];
        }

        return (sortedRates[count / 2 - 1] + sortedRates[count / 2]) / 2m;
    }
}
