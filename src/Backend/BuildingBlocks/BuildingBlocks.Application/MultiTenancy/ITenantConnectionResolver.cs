using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.MultiTenancy;

/// <summary>
/// Resolves and provides decrypted database connection strings for SaaS tenants.
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>
    /// Resolves the database connection string for a specific tenant by its identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the decrypted connection string or an error if not found or inactive.</returns>
    Task<Result<string>> ResolveConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the database connection string for a tenant identified by its subdomain or slug.
    /// </summary>
    /// <param name="subdomain">The unique subdomain or slug of the tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the decrypted connection string or an error.</returns>
    Task<Result<string>> ResolveConnectionStringBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the database connection string for the current contextual tenant from the active request scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the decrypted connection string for the contextual tenant or an error.</returns>
    Task<Result<string>> ResolveCurrentTenantConnectionStringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached connection strings for the specified tenant identifier.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    void InvalidateCache(Guid tenantId);

    /// <summary>
    /// Invalidates cached connection strings for the specified tenant subdomain.
    /// </summary>
    /// <param name="subdomain">The unique subdomain of the tenant.</param>
    void InvalidateCacheBySubdomain(string subdomain);
}
