namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Defines hierarchical and functional roles for operational tenant users.
/// </summary>
public enum TenantRole
{
    /// <summary>
    /// Agency owner with root tenant administrative permissions and billing control.
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Tenant administrator managing team members, settings, and workspace allocations.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Squad leader directing media managers and analysts across designated workspaces.
    /// </summary>
    SquadLeader = 3,

    /// <summary>
    /// Media manager executing paid traffic campaigns across advertising channels.
    /// </summary>
    MediaManager = 4,

    /// <summary>
    /// Performance analyst auditing metrics, attribution models, and marketing efficiency.
    /// </summary>
    Analyst = 5,

    /// <summary>
    /// Read-only external client or guest user observing performance dashboards.
    /// </summary>
    Guest = 6
}
