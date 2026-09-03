namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Marker entity used to verify tenant database schema existence and health checks.
/// </summary>
public sealed class TenantSchemaMarker
{
    /// <summary>
    /// Gets or sets the marker identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the marker label or descriptive name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
