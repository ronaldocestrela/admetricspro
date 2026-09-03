using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Plans.Events;

/// <summary>
/// Domain event raised when a subscription plan is updated.
/// </summary>
/// <param name="PlanId">Identifier of the updated plan.</param>
/// <param name="Name">Updated name of the plan.</param>
/// <param name="MonthlyPrice">Updated monthly price.</param>
public sealed record PlanUpdatedDomainEvent(
    PlanId PlanId,
    string Name,
    decimal MonthlyPrice) : IDomainEvent;
