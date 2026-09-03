namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Scoped holder for resolved tenant connection strings to support fast scoped DbContext instantiation.
/// </summary>
public interface ITenantConnectionHolder
{
    /// <summary>
    /// Gets the resolved connection string, or null if not yet resolved.
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>
    /// Sets the resolved connection string for the current scope.
    /// </summary>
    /// <param name="connectionString">The plain database connection string.</param>
    void SetConnectionString(string connectionString);
}
