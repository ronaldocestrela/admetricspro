using BuildingBlocks.Application.Messaging;
using Master.Domain.FeatureFlags;

namespace Master.Application.FeatureFlags.Commands.CreateFeatureFlag;

/// <summary>
/// Command to create and register a new feature flag or operational kill switch in the Master catalog.
/// </summary>
/// <param name="Key">Unique key (e.g. "feature.analytics.mer-v2").</param>
/// <param name="Name">Human readable name.</param>
/// <param name="Description">Description.</param>
/// <param name="IsEnabled">Initial toggle state.</param>
/// <param name="IsKillSwitch">Whether this flag functions as an operational emergency Kill Switch.</param>
/// <param name="TargetingType">Targeting model.</param>
/// <param name="RolloutPercentage">Rollout percentage (0-100).</param>
/// <param name="TargetTenantIds">Optional allowlist of targeted tenants.</param>
/// <param name="CreatedBy">Operator/user creating the record.</param>
public sealed record CreateFeatureFlagCommand(
    string Key,
    string Name,
    string Description,
    bool IsEnabled,
    bool IsKillSwitch,
    FeatureFlagTargetingType TargetingType,
    int RolloutPercentage,
    IEnumerable<Guid>? TargetTenantIds,
    string CreatedBy) : ICommand<Guid>;
