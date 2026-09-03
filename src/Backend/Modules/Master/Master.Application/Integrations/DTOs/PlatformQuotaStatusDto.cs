using Master.Domain.Integrations;

namespace Master.Application.Integrations.DTOs;

/// <summary>
/// Data transfer object representing the real-time operational rate limit and quota status of an ad platform.
/// </summary>
/// <param name="Platform">Ad platform identifier.</param>
/// <param name="PlatformName">Display name of the platform (e.g. "Meta Graph API").</param>
/// <param name="MaxLimit">Maximum quota limit configured for the active window.</param>
/// <param name="CurrentConsumption">Total consumed operations/calls in current window.</param>
/// <param name="UsagePercentage">Percentage of quota utilized.</param>
/// <param name="AlertLevel">Current operational warning status.</param>
/// <param name="IsWarning">Indicates whether usage has reached or exceeded 80%.</param>
/// <param name="WindowDuration">Duration of the tracking window.</param>
/// <param name="WindowStartUtc">Start timestamp of the active window in UTC.</param>
/// <param name="LastUpdatedUtc">Timestamp of the last recorded operation in UTC.</param>
public sealed record PlatformQuotaStatusDto(
    AdPlatform Platform,
    string PlatformName,
    long MaxLimit,
    long CurrentConsumption,
    double UsagePercentage,
    QuotaAlertLevel AlertLevel,
    bool IsWarning,
    TimeSpan WindowDuration,
    DateTime WindowStartUtc,
    DateTime LastUpdatedUtc);
