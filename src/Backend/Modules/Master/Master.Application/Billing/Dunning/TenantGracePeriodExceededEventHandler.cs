using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants.Events;
using Microsoft.Extensions.Logging;

namespace Master.Application.Billing.Dunning;

/// <summary>
/// Handles <see cref="TenantGracePeriodExceededEvent"/> in-memory domain events to record audit logs and trigger cross-module reactive flows.
/// </summary>
public sealed class TenantGracePeriodExceededEventHandler : IDomainEventHandler<TenantGracePeriodExceededEvent>
{
    private readonly ILogger<TenantGracePeriodExceededEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantGracePeriodExceededEventHandler"/> class.
    /// </summary>
    /// <param name="logger">Structured logger instance.</param>
    public TenantGracePeriodExceededEventHandler(ILogger<TenantGracePeriodExceededEventHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Handle(DomainEventNotification<TenantGracePeriodExceededEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogWarning(
            "Tenant {TenantId} exceeded grace period. Stage transitioned from {PreviousStage} to {CurrentStage}. Days overdue: {DaysOverdue}. Original due date: {DueDateUtc}",
            domainEvent.TenantId.Value,
            domainEvent.PreviousStage,
            domainEvent.CurrentStage,
            domainEvent.DaysOverdue,
            domainEvent.DueDateUtc);

        return Task.CompletedTask;
    }
}
