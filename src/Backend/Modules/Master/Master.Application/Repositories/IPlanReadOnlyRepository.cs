using Master.Application.Plans.DTOs;
using Master.Domain.Plans;

namespace Master.Application.Repositories;

/// <summary>
/// Read-only repository contract for projection queries on subscription plans.
/// </summary>
public interface IPlanReadOnlyRepository
{
    /// <summary>
    /// Lists all subscription plans projected to DTOs.
    /// </summary>
    /// <param name="includeInactive">Whether to include deactivated plans.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of plan DTOs.</returns>
    Task<IReadOnlyList<PlanDto>> ListAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single subscription plan by its identifier projected to DTO.
    /// </summary>
    /// <param name="id">Plan identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plan DTO if found; otherwise null.</returns>
    Task<PlanDto?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default);
}
