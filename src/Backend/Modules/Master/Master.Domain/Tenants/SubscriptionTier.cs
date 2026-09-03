namespace Master.Domain.Tenants;

/// <summary>
/// Defines the subscription tier levels available for SaaS tenants.
/// </summary>
public enum SubscriptionTier
{
    /// <summary>
    /// Free trial period with feature and time limits.
    /// </summary>
    Trial = 0,

    /// <summary>
    /// Starter plan designed for individual media managers and small agencies.
    /// </summary>
    Starter = 1,

    /// <summary>
    /// Professional plan with multi-squad support and cross-network automations.
    /// </summary>
    Pro = 2,

    /// <summary>
    /// Enterprise tier with full white-label, dedicated cluster support and custom SLAs.
    /// </summary>
    Enterprise = 3
}
