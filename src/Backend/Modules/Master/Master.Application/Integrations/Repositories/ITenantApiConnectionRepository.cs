using Master.Domain.Integrations;

namespace Master.Application.Integrations.Repositories;

/// <summary>
/// Repository interface for querying and persisting tenant ad platform integration connections and token health.
/// </summary>
public interface ITenantApiConnectionRepository
{
    /// <summary>
    /// Retrieves a connection by its unique identifier.
    /// </summary>
    /// <param name="id">Connection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching connection entity, or null if not found.</returns>
    Task<TenantApiConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tenant connections with optional platform and status filtering.
    /// </summary>
    /// <param name="platform">Optional platform filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of connections matching filters.</returns>
    Task<IReadOnlyList<TenantApiConnection>> GetConnectionsAsync(
        AdPlatform? platform = null,
        ApiConnectionStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts total connections currently having the specified status.
    /// </summary>
    /// <param name="status">Target connection status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of matching connections.</returns>
    Task<int> CountByStatusAsync(ApiConnectionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of all tenant integration connections in the catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of connections.</returns>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new tenant API connection.
    /// </summary>
    /// <param name="connection">Connection entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(TenantApiConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant API connection.
    /// </summary>
    /// <param name="connection">Connection entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(TenantApiConnection connection, CancellationToken cancellationToken = default);
}
