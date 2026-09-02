using BuildingBlocks.Application.Persistence;
using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Repository contract for tenant aggregate persistence.
/// </summary>
public interface ITenantRepository : IRepository<Tenant, TenantId>
{
}