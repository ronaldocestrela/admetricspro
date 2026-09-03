using BuildingBlocks.Domain.Abstractions;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Wraps a domain event as a MediatR notification for in-memory publishing.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type.</typeparam>
/// <param name="DomainEvent">The underlying domain event payload.</param>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;

/// <summary>
/// Defines a handler for in-memory domain events wrapped as MediatR notifications.
/// </summary>
/// <typeparam name="TDomainEvent">The underlying domain event type.</typeparam>
public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent;
