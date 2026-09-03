using Master.Application.Plans.DTOs;
using Master.Application.Repositories;
using Master.Domain.Plans;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPlanReadOnlyRepository"/> optimized for query projections without tracking.
/// </summary>
public sealed class PlanReadOnlyRepository : IPlanReadOnlyRepository
{
    private readonly MasterDbContext _masterDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanReadOnlyRepository"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog DbContext instance.</param>
    public PlanReadOnlyRepository(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext ?? throw new ArgumentNullException(nameof(masterDbContext));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanDto>> ListAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _masterDbContext.Plans.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.MonthlyPrice)
            .Select(p => new PlanDto(
                p.Id.Value,
                p.Name,
                p.Description,
                p.Tier.ToString(),
                p.MonthlyPrice,
                p.AnnualDiscountPercentage,
                p.Limits.MaxSeats,
                p.Limits.MaxWorkspaces,
                p.Limits.MonthlyAdSpendCap,
                p.Features.HasWhiteLabel,
                p.Features.HasCustomCname,
                p.Features.HasAiCopilot,
                p.Features.HasCrossNetworkAutomations,
                p.IsActive,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<PlanDto?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _masterDbContext.Plans
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PlanDto(
                p.Id.Value,
                p.Name,
                p.Description,
                p.Tier.ToString(),
                p.MonthlyPrice,
                p.AnnualDiscountPercentage,
                p.Limits.MaxSeats,
                p.Limits.MaxWorkspaces,
                p.Limits.MonthlyAdSpendCap,
                p.Features.HasWhiteLabel,
                p.Features.HasCustomCname,
                p.Features.HasAiCopilot,
                p.Features.HasCrossNetworkAutomations,
                p.IsActive,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
