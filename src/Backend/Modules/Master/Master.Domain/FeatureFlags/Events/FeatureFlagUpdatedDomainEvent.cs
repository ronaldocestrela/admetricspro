using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.FeatureFlags.Events;

/// <summary>
/// Domain event emitted when a feature flag configuration (rollout percentage, allowlist, or toggle) is updated.
/// </summary>
/// <param name="Key">Unique key of the feature flag.</param>
/// <param name="IsEnabled">Current general status.</param>
/// <param name="RolloutPercentage">Current rollout percentage.</param>
/// <param name="UpdatedBy">Identifier or email of the user who modified the flag.</param>
/// <param name="UpdatedAtUtc">Timestamp of update.</param>
public sealed record FeatureFlagUpdatedDomainEvent(
    string Key,
    bool IsEnabled,
    int RolloutPercentage,
    string? UpdatedBy,
    DateTime UpdatedAtUtc) : IDomainEvent;
