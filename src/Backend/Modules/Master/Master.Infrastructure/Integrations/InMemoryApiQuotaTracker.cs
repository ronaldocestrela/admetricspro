using System.Collections.Concurrent;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Repositories;
using Master.Application.Integrations.Services;
using Master.Domain.Integrations;

namespace Master.Infrastructure.Integrations;

/// <summary>
/// Thread-safe in-memory rate limit and quota tracking service.
/// Provides low-latency counter accumulation across Meta, Google, TikTok, and Bing Ads APIs,
/// automatically firing preventive threshold events when usage reaches or exceeds 80%.
/// </summary>
public sealed class InMemoryApiQuotaTracker : IApiQuotaTrackerService
{
    private static readonly ConcurrentDictionary<AdPlatform, ApiQuotaTracker> _trackers = new();
    private static readonly object _syncLock = new();
    private readonly IApiQuotaRepository? _repository;

    static InMemoryApiQuotaTracker()
    {
        InitializeDefaultTrackers();
    }

    /// <summary>
    /// Resets all in-memory trackers to their initial default configurations.
    /// </summary>
    public static void ResetAll()
    {
        _trackers.Clear();
        InitializeDefaultTrackers();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryApiQuotaTracker"/> class.
    /// </summary>
    /// <param name="repository">Optional repository for database persistence.</param>
    public InMemoryApiQuotaTracker(IApiQuotaRepository? repository = null)
    {
        _repository = repository;
    }

    private static void InitializeDefaultTrackers()
    {
        // Meta Graph API: 100,000 calls / 1 hour
        _trackers.TryAdd(AdPlatform.Meta, ApiQuotaTracker.Create(
            AdPlatform.Meta,
            maxLimit: 100000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0).Value);

        // Google Ads API: 500,000 operations / 24 hours
        _trackers.TryAdd(AdPlatform.Google, ApiQuotaTracker.Create(
            AdPlatform.Google,
            maxLimit: 500000,
            windowDuration: TimeSpan.FromHours(24),
            warningThresholdPercentage: 80.0).Value);

        // TikTok Marketing API: 60,000 requests / 1 hour
        _trackers.TryAdd(AdPlatform.TikTok, ApiQuotaTracker.Create(
            AdPlatform.TikTok,
            maxLimit: 60000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0).Value);

        // Bing Ads API: 30,000 requests / 1 hour
        _trackers.TryAdd(AdPlatform.Bing, ApiQuotaTracker.Create(
            AdPlatform.Bing,
            maxLimit: 30000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0).Value);
    }

    /// <inheritdoc />
    public async Task<Result<PlatformQuotaStatusDto>> RecordUsageAsync(
        AdPlatform platform,
        long units,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (units <= 0)
        {
            return Result<PlatformQuotaStatusDto>.Failure(
                Error.Validation("ApiQuota.InvalidUnits", "O consumo registrado deve ser maior que zero."));
        }

        var tracker = _trackers.GetOrAdd(platform, p =>
            ApiQuotaTracker.Create(p, 50000, TimeSpan.FromHours(1), 80.0, windowStartUtc: nowUtc).Value);

        lock (_syncLock)
        {
            if (nowUtc < tracker.WindowStartUtc || nowUtc - tracker.WindowStartUtc >= tracker.WindowDuration)
            {
                tracker.ResetWindow(nowUtc);
            }

            var recordResult = tracker.RecordUsage(units, nowUtc);
            if (recordResult.IsFailure)
            {
                return Result<PlatformQuotaStatusDto>.Failure(recordResult.Error);
            }
        }

        if (_repository is not null)
        {
            try
            {
                await _repository.UpdateAsync(tracker, cancellationToken);
            }
            catch
            {
                // In-memory counters remain resilient even if persistence fails temporarily
            }
        }

        return Result<PlatformQuotaStatusDto>.Success(MapToDto(tracker));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PlatformQuotaStatusDto>> GetAllQuotaStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var list = new List<PlatformQuotaStatusDto>();

        foreach (var platform in new[] { AdPlatform.Meta, AdPlatform.Google, AdPlatform.TikTok, AdPlatform.Bing })
        {
            if (_trackers.TryGetValue(platform, out var tracker))
            {
                lock (_syncLock)
                {
                    if (now - tracker.WindowStartUtc >= tracker.WindowDuration)
                    {
                        tracker.ResetWindow(now);
                    }
                }
                list.Add(MapToDto(tracker));
            }
        }

        return Task.FromResult<IReadOnlyList<PlatformQuotaStatusDto>>(list);
    }

    /// <inheritdoc />
    public Task<PlatformQuotaStatusDto?> GetPlatformQuotaStatusAsync(
        AdPlatform platform,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (_trackers.TryGetValue(platform, out var tracker))
        {
            lock (_syncLock)
            {
                if (now - tracker.WindowStartUtc >= tracker.WindowDuration)
                {
                    tracker.ResetWindow(now);
                }
            }
            return Task.FromResult<PlatformQuotaStatusDto?>(MapToDto(tracker));
        }

        return Task.FromResult<PlatformQuotaStatusDto?>(null);
    }

    private static PlatformQuotaStatusDto MapToDto(ApiQuotaTracker tracker)
    {
        return new PlatformQuotaStatusDto(
            Platform: tracker.Platform,
            PlatformName: FormatPlatformName(tracker.Platform),
            MaxLimit: tracker.MaxLimit,
            CurrentConsumption: tracker.CurrentConsumption,
            UsagePercentage: tracker.UsagePercentage,
            AlertLevel: tracker.AlertLevel,
            IsWarning: tracker.AlertLevel >= QuotaAlertLevel.Warning,
            WindowDuration: tracker.WindowDuration,
            WindowStartUtc: tracker.WindowStartUtc,
            LastUpdatedUtc: tracker.LastUpdatedUtc);
    }

    private static string FormatPlatformName(AdPlatform platform) => platform switch
    {
        AdPlatform.Meta => "Meta Graph API",
        AdPlatform.Google => "Google Ads API",
        AdPlatform.TikTok => "TikTok Marketing API",
        AdPlatform.Bing => "Bing Ads API",
        _ => platform.ToString()
    };
}
