namespace BuildingBlocks.Application.MultiTenancy;

/// <summary>
/// Provides access to and mutation of the active <see cref="ITenantContext"/> within the current execution scope.
/// </summary>
public interface ITenantContextAccessor
{
    /// <summary>
    /// Gets or sets the active tenant context.
    /// </summary>
    ITenantContext TenantContext { get; set; }
}
