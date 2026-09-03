using Master.Application.Repositories;
using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITenantReadOnlyRepository"/> optimized for projection without entity tracking.
/// </summary>
public sealed class TenantReadOnlyRepository : ITenantReadOnlyRepository
{
    private readonly MasterDbContext _masterDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantReadOnlyRepository"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog DbContext instance.</param>
    public TenantReadOnlyRepository(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext;
    }

    /// <inheritdoc />
    public Task<TenantDetailsResponse?> GetDetailsByIdAsync(TenantId id, CancellationToken cancellationToken = default)
    {
        return _masterDbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TenantDetailsResponse(
                t.Id.Value,
                t.CompanyName,
                t.Cnpj,
                t.Subdomain,
                t.Status.ToString(),
                t.Tier.ToString(),
                t.SubscriptionExpiresAtUtc,
                t.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantDetailsResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _masterDbContext.Tenants
            .AsNoTracking()
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantDetailsResponse(
                t.Id.Value,
                t.CompanyName,
                t.Cnpj,
                t.Subdomain,
                t.Status.ToString(),
                t.Tier.ToString(),
                t.SubscriptionExpiresAtUtc,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken = default)
    {
        return _masterDbContext.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == id, cancellationToken);
    }
}
