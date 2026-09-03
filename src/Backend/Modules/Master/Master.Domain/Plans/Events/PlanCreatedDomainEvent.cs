using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Plans.Events;

/// <summary>
/// Domain event raised when a new subscription plan is created in the master catalog.
/// </summary>
/// <param name="PlanId">Identifier of the created plan.</param>
/// <param name="Name">Name of the plan.</param>
/// <param name="Tier">Associated tier level.</param>
public sealed record PlanCreatedDomainEvent(
    PlanId PlanId,
    string Name,
    Tenants.SubscriptionTier Tier) : IDomainEvent;
