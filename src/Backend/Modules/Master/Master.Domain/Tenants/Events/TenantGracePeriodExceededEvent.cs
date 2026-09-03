using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Tenants.Events;

/// <summary>
/// Domain event emitted when a tenant exceeds the grace period or transitions between dunning stages.
/// </summary>
/// <param name="TenantId">The affected tenant identifier.</param>
/// <param name="PreviousStage">The prior dunning stage before evaluation.</param>
/// <param name="CurrentStage">The newly evaluated dunning stage.</param>
/// <param name="DaysOverdue">Total count of calendar days since payment due date.</param>
/// <param name="DueDateUtc">The original payment due timestamp in UTC.</param>
public sealed record TenantGracePeriodExceededEvent(
    TenantId TenantId,
    DunningStage PreviousStage,
    DunningStage CurrentStage,
    int DaysOverdue,
    DateTime DueDateUtc) : IDomainEvent;
