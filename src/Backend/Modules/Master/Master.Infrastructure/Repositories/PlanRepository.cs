using Master.Application.Repositories;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for <see cref="SubscriptionPlan"/> aggregate persistence.
/// </summary>
public sealed class PlanRepository : IPlanRepository
{
    private readonly MasterDbContext _masterDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanRepository"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog database context.</param>
    public PlanRepository(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext ?? throw new ArgumentNullException(nameof(masterDbContext));
    }

    /// <inheritdoc />
    public async Task AddAsync(SubscriptionPlan entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _masterDbContext.Plans.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SubscriptionPlan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _masterDbContext.Plans.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier, CancellationToken cancellationToken = default)
    {
        return _masterDbContext.Plans
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(p => p.Tier == tier && p.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, PlanId? excludePlanId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        var query = _masterDbContext.Plans.AsQueryable();

        if (excludePlanId is not null)
        {
            query = query.Where(p => p.Id != excludePlanId);
        }

        return query.AnyAsync(p => p.Name.ToLower() == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(SubscriptionPlan entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _masterDbContext.Plans.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(SubscriptionPlan entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _masterDbContext.Plans.Remove(entity);
    }
}
