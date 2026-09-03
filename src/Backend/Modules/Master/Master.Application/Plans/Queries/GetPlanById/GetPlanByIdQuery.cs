using BuildingBlocks.Application.Messaging;
using Master.Application.Plans.DTOs;

namespace Master.Application.Plans.Queries.GetPlanById;

/// <summary>
/// Query to retrieve a single subscription plan by its identifier.
/// </summary>
/// <param name="PlanId">Plan unique identifier.</param>
public sealed record GetPlanByIdQuery(Guid PlanId) : IQuery<PlanDto?>;
