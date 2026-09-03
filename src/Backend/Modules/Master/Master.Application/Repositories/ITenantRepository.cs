using BuildingBlocks.Application.Persistence;
using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Repository contract for tenant aggregate persistence.
/// </summary>
public interface ITenantRepository : IRepository<Tenant, TenantId>
{
    /// <summary>
    /// Finds a tenant by its normalized subdomain.
    /// </summary>
    /// <param name="subdomain">Tenant subdomain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant aggregate if found; otherwise null.</returns>
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
}