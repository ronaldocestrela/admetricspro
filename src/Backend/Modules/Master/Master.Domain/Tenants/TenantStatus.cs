namespace Master.Domain.Tenants;

/// <summary>
/// Represents tenant lifecycle states in the master catalog.
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant is active and fully operational.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Tenant is within trial period.
    /// </summary>
    Trial = 2,

    /// <summary>
    /// Tenant is suspended.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Tenant is canceled.
    /// </summary>
    Cancelled = 4
}