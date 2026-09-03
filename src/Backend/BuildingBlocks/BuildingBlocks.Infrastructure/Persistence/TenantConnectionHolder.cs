namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Scoped holder implementation storing resolved tenant connection string for the current execution scope.
/// </summary>
public sealed class TenantConnectionHolder : ITenantConnectionHolder
{
    /// <inheritdoc />
    public string? ConnectionString { get; private set; }

    /// <inheritdoc />
    public void SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
    }
}
