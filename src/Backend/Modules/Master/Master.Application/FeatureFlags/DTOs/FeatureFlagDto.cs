using Master.Domain.FeatureFlags;

namespace Master.Application.FeatureFlags.DTOs;

/// <summary>
/// Data Transfer Object representing a feature flag or kill switch for API consumers and management UI.
/// </summary>
/// <param name="Id">Primary key identifier.</param>
/// <param name="Key">Unique string key (e.g., "killswitch.automation.global").</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Description of the feature or impacted subsystem.</param>
/// <param name="IsEnabled">Status flag. For kill switches, true means the switch is ENGAGED (frozen).</param>
/// <param name="IsKillSwitch">Indicates whether this record is an emergency kill switch.</param>
/// <param name="TargetingType">Targeting model (Global, PercentageRollout, TenantList).</param>
/// <param name="RolloutPercentage">Rollout percentage (0-100) when targeting is percentage-based.</param>
/// <param name="TargetTenantIds">Collection of explicitly allowed tenant IDs.</param>
/// <param name="KillSwitchActivatedAtUtc">Timestamp of last activation.</param>
/// <param name="KillSwitchReason">Operational justification.</param>
/// <param name="KillSwitchTriggeredBy">Operator/service who triggered the switch.</param>
/// <param name="CreatedBy">Original creator.</param>
/// <param name="CreatedAtUtc">Creation timestamp.</param>
/// <param name="UpdatedAtUtc">Last update timestamp.</param>
/// <param name="UpdatedBy">Last modifier.</param>
public sealed record FeatureFlagDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    bool IsEnabled,
    bool IsKillSwitch,
    FeatureFlagTargetingType TargetingType,
    int RolloutPercentage,
    IReadOnlyCollection<Guid> TargetTenantIds,
    DateTime? KillSwitchActivatedAtUtc,
    string? KillSwitchReason,
    string? KillSwitchTriggeredBy,
    string CreatedBy,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? UpdatedBy);
