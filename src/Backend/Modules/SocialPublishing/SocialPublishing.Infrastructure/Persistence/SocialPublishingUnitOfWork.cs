using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using SocialPublishing.Application.Abstractions;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Consolida alterações no <see cref="SocialPublishingDbContext"/> e publica eventos de domínio via MediatR.
/// </summary>
public sealed class SocialPublishingUnitOfWork : ISocialPublishingUnitOfWork
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;
    private readonly IPublisher? _publisher;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingUnitOfWork"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    /// <param name="publisher">Publicador in-memory de notificações MediatR.</param>
    public SocialPublishingUnitOfWork(
        SocialPublishingDbContextAccessor contextAccessor,
        IPublisher? publisher = null)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _publisher = publisher;
    }

    /// <inheritdoc />
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        var context = contextResult.Value;
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        var affected = await context.SaveChangesAsync(cancellationToken);

        if (_publisher is not null)
        {
            foreach (var domainEvent in domainEvents)
            {
                await PublishDomainEventAsync(domainEvent, cancellationToken);
            }
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return affected;
    }

    private async Task PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = Activator.CreateInstance(notificationType, domainEvent);
        if (notification is INotification mediatRNotification)
        {
            await _publisher!.Publish(mediatRNotification, cancellationToken);
        }
    }
}
