namespace Master.Domain.FeatureFlags;

/// <summary>
/// Defines how a feature flag evaluates which tenants have access to the feature.
/// </summary>
public enum FeatureFlagTargetingType
{
    /// <summary>
    /// Evaluates universally across all tenants based purely on the IsEnabled flag.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Staged progressive percentage rollout (0% to 100%) calculated deterministically by TenantId.
    /// </summary>
    PercentageRollout = 1,

    /// <summary>
    /// Explicit tenant allowlist targeting specific individual tenants.
    /// </summary>
    TenantList = 2
}
