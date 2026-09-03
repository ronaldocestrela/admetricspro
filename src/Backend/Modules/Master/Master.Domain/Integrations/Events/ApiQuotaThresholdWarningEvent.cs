using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Integrations.Events;

/// <summary>
/// Domain event emitted when an API's consumption reaches or exceeds the preventive or critical threshold.
/// </summary>
/// <param name="Platform">Ad platform whose quota was triggered.</param>
/// <param name="AlertLevel">Warning or Critical level.</param>
/// <param name="CurrentConsumption">Total consumed operations in the active window.</param>
/// <param name="MaxLimit">Maximum quota limit configured for the window.</param>
/// <param name="UsagePercentage">Current utilization percentage.</param>
/// <param name="OccurredAtUtc">Timestamp when threshold was reached.</param>
public sealed record ApiQuotaThresholdWarningEvent(
    AdPlatform Platform,
    QuotaAlertLevel AlertLevel,
    long CurrentConsumption,
    long MaxLimit,
    double UsagePercentage,
    DateTime OccurredAtUtc) : IDomainEvent;
