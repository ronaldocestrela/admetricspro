using BuildingBlocks.Application.Messaging;
using Master.Application.Plans.DTOs;

namespace Master.Application.Plans.Queries.GetPlans;

/// <summary>
/// Query to retrieve all subscription plans from the catalog.
/// </summary>
/// <param name="IncludeInactive">Whether to include deactivated plans.</param>
public sealed record GetPlansQuery(bool IncludeInactive = false) : IQuery<IReadOnlyList<PlanDto>>;
