using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Grava linhas de outbox no <see cref="DbContext"/> transacional do escopo atual.
/// </summary>
public sealed class DbIntegrationOutboxWriter(DbContext context)
    : IIntegrationOutboxWriter, ICatalogIntegrationOutboxWriter
{
    /// <inheritdoc />
    public async Task<Result> AddAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var duplicate = await context.Set<IntegrationOutboxMessage>()
            .AsNoTracking()
            .AnyAsync(m => m.EventId == message.EventId, cancellationToken);

        if (duplicate)
        {
            return Result.Failure(Error.Conflict("Messaging.DuplicateEventId", "Já existe outbox para este EventId."));
        }

        await context.Set<IntegrationOutboxMessage>().AddAsync(message, cancellationToken);
        return Result.Success();
    }
}