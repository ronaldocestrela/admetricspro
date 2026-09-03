using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Read-only repository contract for analytical and directory queries over tenants in the master catalog.
/// </summary>
public interface ITenantReadOnlyRepository
{
    /// <summary>
    /// Retrieves tenant directory details by identifier without tracking or exposing sensitive connection credentials.
    /// </summary>
    /// <param name="id">The unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant details projection if found; otherwise null.</returns>
    Task<TenantDetailsResponse?> GetDetailsByIdAsync(TenantId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tenants in the master directory projected into safe response models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of tenant details.</returns>
    Task<IReadOnlyList<TenantDetailsResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a tenant with the specified identifier exists in the master catalog.
    /// </summary>
    /// <param name="id">The unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the tenant exists; otherwise false.</returns>
    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken = default);
}
