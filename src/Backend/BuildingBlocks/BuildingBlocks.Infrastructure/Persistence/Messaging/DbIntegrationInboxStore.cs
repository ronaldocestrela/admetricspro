using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Store EF de inbox com unicidade <c>(EventId, ConsumerName)</c>.
/// </summary>
public sealed class DbIntegrationInboxStore(DbContext context) : IIntegrationInboxStore
{
    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await context.Set<IntegrationInboxMessage>()
            .AsNoTracking()
            .AnyAsync(i => i.EventId == eventId && i.ConsumerName == consumerName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> TryRecordAsync(
        Guid eventId,
        string consumerName,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await ExistsAsync(eventId, consumerName, cancellationToken))
        {
            return Result.Failure(Error.Conflict("Messaging.AlreadyInInbox", "Evento já processado por este consumidor."));
        }

        var create = IntegrationInboxMessage.Create(Guid.NewGuid(), eventId, consumerName, processedAtUtc);
        if (create.IsFailure)
        {
            return Result.Failure(create.Error);
        }

        await context.Set<IntegrationInboxMessage>().AddAsync(create.Value, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(Error.Conflict("Messaging.AlreadyInInbox", "Evento já processado por este consumidor."));
        }
    }
}