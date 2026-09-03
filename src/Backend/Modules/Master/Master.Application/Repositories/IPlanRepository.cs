using BuildingBlocks.Application.Persistence;
using Master.Domain.Plans;
using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Repository contract for <see cref="SubscriptionPlan"/> aggregate persistence.
/// </summary>
public interface IPlanRepository : IRepository<SubscriptionPlan, PlanId>
{
    /// <summary>
    /// Checks if a plan with the given name already exists.
    /// </summary>
    /// <param name="name">Plan name to check.</param>
    /// <param name="excludePlanId">Optional plan ID to exclude (used when updating).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a plan with the name exists; otherwise false.</returns>
    Task<bool> ExistsByNameAsync(string name, PlanId? excludePlanId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a plan by its tier classification.
    /// </summary>
    /// <param name="tier">Tier level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plan if found; otherwise null.</returns>
    Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier, CancellationToken cancellationToken = default);
}
