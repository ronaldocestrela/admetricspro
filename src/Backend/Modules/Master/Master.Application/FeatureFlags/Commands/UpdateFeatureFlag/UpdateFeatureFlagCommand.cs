using BuildingBlocks.Application.Messaging;
using Master.Domain.FeatureFlags;

namespace Master.Application.FeatureFlags.Commands.UpdateFeatureFlag;

/// <summary>
/// Command to update the configuration, rollout percentage, or allowlist of an existing feature flag.
/// </summary>
/// <param name="Id">Primary identifier of the feature flag.</param>
/// <param name="IsEnabled">New active status.</param>
/// <param name="TargetingType">Targeting model.</param>
/// <param name="RolloutPercentage">Rollout percentage (0-100).</param>
/// <param name="TargetTenantIds">Optional allowlist of targeted tenants.</param>
/// <param name="UpdatedBy">Operator/user modifying the flag.</param>
public sealed record UpdateFeatureFlagCommand(
    Guid Id,
    bool IsEnabled,
    FeatureFlagTargetingType TargetingType,
    int RolloutPercentage,
    IEnumerable<Guid>? TargetTenantIds,
    string UpdatedBy) : ICommand;
