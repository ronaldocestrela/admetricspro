using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for tenant aggregate persistence.
/// </summary>
public sealed class TenantRepository : ITenantRepository
{
    private readonly MasterDbContext _masterDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantRepository"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog context.</param>
    public TenantRepository(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(Tenant entity, CancellationToken cancellationToken = default)
    {
        await _masterDbContext.Tenants.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default)
    {
        return _masterDbContext.Tenants.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Tenant entity)
    {
        _masterDbContext.Tenants.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(Tenant entity)
    {
        _masterDbContext.Tenants.Remove(entity);
    }
}