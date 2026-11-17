using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Domain.Planning;

/// <summary>
/// Serviço de domínio puro que gera um cronograma editorial determinístico por pilares, fuso e semente.
/// </summary>
public static class EditorialCalendarPlanner
{
    private const int LocalHour = 10;
    private const int LocalMinute = 0;

    private static readonly (OrganicPlatform Platform, OrganicPostFormat Format)[] PlatformFormatCycle =
    [
        (OrganicPlatform.Instagram, OrganicPostFormat.FeedImage),
        (OrganicPlatform.FacebookPage, OrganicPostFormat.FeedImage),
        (OrganicPlatform.Instagram, OrganicPostFormat.Reel),
        (OrganicPlatform.TikTok, OrganicPostFormat.TikTokVideo),
        (OrganicPlatform.Instagram, OrganicPostFormat.FeedCarousel),
    ];

    private static readonly ContentPillar[] PillarTieBreakOrder =
    [
        ContentPillar.Authority,
        ContentPillar.Connection,
        ContentPillar.SocialProof,
        ContentPillar.Sale,
    ];

    /// <summary>
    /// Gera slots editoriais para o intervalo informado, sem texto nem persistência.
    /// </summary>
    /// <param name="brandVoice">DNA de marca com pesos dos pilares; ausência retorna <see cref="SocialErrors.BrandVoiceNotFound"/>.</param>
    /// <param name="rangeStart">Data inicial inclusiva no fuso do workspace.</param>
    /// <param name="rangeEnd">Data final inclusiva no fuso do workspace.</param>
    /// <param name="timeZoneId">Identificador IANA do fuso (ex.: America/Sao_Paulo).</param>
    /// <param name="postsPerWeek">Quantidade máxima de posts por semana (1–7).</param>
    /// <param name="seed">Semente que altera ordem e ciclo de rede/formato, não as contagens de pilares.</param>
    /// <returns>Lista ordenada de slots ou erro de validação/negócio.</returns>
    public static Result<IReadOnlyList<EditorialSlot>> Plan(
        BrandVoiceProfile? brandVoice,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        string timeZoneId,
        int postsPerWeek,
        int seed)
    {
        if (brandVoice is null)
        {
            return Result<IReadOnlyList<EditorialSlot>>.Failure(SocialErrors.BrandVoiceNotFound);
        }

        if (rangeEnd < rangeStart)
        {
            return Result<IReadOnlyList<EditorialSlot>>.Failure(SocialErrors.PlanRangeInvalid);
        }

        if (postsPerWeek is < 1 or > 7)
        {
            return Result<IReadOnlyList<EditorialSlot>>.Failure(SocialErrors.PostsPerWeekInvalid);
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return Result<IReadOnlyList<EditorialSlot>>.Failure(SocialErrors.TimeZoneInvalid);
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return Result<IReadOnlyList<EditorialSlot>>.Failure(SocialErrors.TimeZoneInvalid);
        }
        catch (InvalidTimeZoneException)
        {
            return Result<IReadOnlyList<EditorialSlot>>.Failure(SocialErrors.TimeZoneInvalid);
        }

        var weights = brandVoice.PillarWeights;
        var slots = new List<EditorialSlot>();

        for (var weekMonday = GetWeekMonday(rangeStart); weekMonday <= rangeEnd; weekMonday = weekMonday.AddDays(7))
        {
            if (weekMonday.AddDays(6) < rangeStart)
            {
                continue;
            }

            var dates = CollectWeekDates(weekMonday, rangeStart, rangeEnd, postsPerWeek);
            if (dates.Count == 0)
            {
                continue;
            }

            var pillarCounts = AllocatePillarCounts(dates.Count, weights);
            var pillars = BuildPillarSequence(pillarCounts, seed, dates.Count);
            ApplySaleSpacing(pillars, dates);

            for (var i = 0; i < dates.Count; i++)
            {
                var localDate = dates[i];
                var utcInstant = ToUtcInstant(localDate, timeZone);
                var cycleIndex = (i + seed) % PlatformFormatCycle.Length;
                var (platform, format) = PlatformFormatCycle[cycleIndex];

                slots.Add(new EditorialSlot(localDate, utcInstant, pillars[i], platform, format));
            }
        }

        return Result<IReadOnlyList<EditorialSlot>>.Success(slots);
    }

    private static DateOnly GetWeekMonday(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    private static List<DateOnly> CollectWeekDates(
        DateOnly weekMonday,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        int postsPerWeek)
    {
        var dates = new List<DateOnly>(postsPerWeek);
        for (var offset = 0; offset < 7; offset++)
        {
            var candidate = weekMonday.AddDays(offset);
            if (candidate < rangeStart || candidate > rangeEnd)
            {
                continue;
            }

            dates.Add(candidate);
            if (dates.Count == postsPerWeek)
            {
                break;
            }
        }

        return dates;
    }

    private static Dictionary<ContentPillar, int> AllocatePillarCounts(int slotCount, PillarWeights weights)
    {
        var pillars = new[]
        {
            (ContentPillar.Authority, weights.Authority),
            (ContentPillar.Sale, weights.Sale),
            (ContentPillar.Connection, weights.Connection),
            (ContentPillar.SocialProof, weights.SocialProof),
        };

        var floors = new Dictionary<ContentPillar, int>();
        var remainders = new List<(ContentPillar Pillar, decimal Remainder)>();
        var assigned = 0;

        foreach (var (pillar, weight) in pillars)
        {
            var quota = slotCount * (decimal)weight / 100m;
            var floor = (int)decimal.Floor(quota);
            floors[pillar] = floor;
            assigned += floor;
            remainders.Add((pillar, quota - floor));
        }

        var extra = slotCount - assigned;
        if (extra > 0)
        {
            var winners = remainders
                .OrderByDescending(r => r.Remainder)
                .ThenBy(r => Array.IndexOf(PillarTieBreakOrder, r.Pillar))
                .Take(extra)
                .Select(r => r.Pillar)
                .ToHashSet();

            foreach (var pillar in winners)
            {
                floors[pillar]++;
            }
        }

        return floors;
    }

    private static List<ContentPillar> BuildPillarSequence(
        Dictionary<ContentPillar, int> counts,
        int seed,
        int slotCount)
    {
        var sequence = new List<ContentPillar>(slotCount);
        AppendPillar(sequence, ContentPillar.Authority, counts[ContentPillar.Authority]);
        AppendPillar(sequence, ContentPillar.Sale, counts[ContentPillar.Sale]);
        AppendPillar(sequence, ContentPillar.Connection, counts[ContentPillar.Connection]);
        AppendPillar(sequence, ContentPillar.SocialProof, counts[ContentPillar.SocialProof]);

        if (sequence.Count == 0)
        {
            return sequence;
        }

        var rotation = ((seed % sequence.Count) + sequence.Count) % sequence.Count;
        if (rotation > 0)
        {
            var rotated = sequence.Skip(rotation).Concat(sequence.Take(rotation)).ToList();
            sequence.Clear();
            sequence.AddRange(rotated);
        }

        return sequence;
    }

    private static void AppendPillar(List<ContentPillar> sequence, ContentPillar pillar, int count)
    {
        for (var i = 0; i < count; i++)
        {
            sequence.Add(pillar);
        }
    }

    private static void ApplySaleSpacing(List<ContentPillar> pillars, IReadOnlyList<DateOnly> dates)
    {
        for (var i = 0; i < pillars.Count - 1; i++)
        {
            if (pillars[i] != ContentPillar.Sale || pillars[i + 1] != ContentPillar.Sale)
            {
                continue;
            }

            if (dates[i + 1].DayNumber - dates[i].DayNumber != 1)
            {
                continue;
            }

            var swapped = false;
            for (var j = i + 2; j < pillars.Count; j++)
            {
                if (pillars[j] == ContentPillar.Sale)
                {
                    continue;
                }

                (pillars[i + 1], pillars[j]) = (pillars[j], pillars[i + 1]);
                swapped = true;
                break;
            }

            if (swapped)
            {
                continue;
            }

            for (var j = i - 1; j >= 0; j--)
            {
                if (pillars[j] == ContentPillar.Sale)
                {
                    continue;
                }

                (pillars[i + 1], pillars[j]) = (pillars[j], pillars[i + 1]);
                break;
            }
        }
    }

    private static DateTime ToUtcInstant(DateOnly localDate, TimeZoneInfo timeZone)
    {
        var local = new DateTime(localDate.Year, localDate.Month, localDate.Day, LocalHour, LocalMinute, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
