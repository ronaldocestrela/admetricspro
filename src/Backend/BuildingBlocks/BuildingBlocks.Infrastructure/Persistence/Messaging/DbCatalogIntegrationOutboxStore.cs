using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Store EF de outbox do catálogo Master para reagendamento pós-consumo.
/// </summary>
public sealed class DbCatalogIntegrationOutboxStore(DbContext context) : ICatalogIntegrationOutboxStore
{
    /// <inheritdoc />
    public async Task<IntegrationOutboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await context.Set<IntegrationOutboxMessage>()
            .FirstOrDefaultAsync(m => m.EventId == eventId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Set<IntegrationOutboxMessage>().Update(message);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegrationOutboxMessage>> ListDeadLettersAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var take = Math.Clamp(limit, 1, 500);
        return await context.Set<IntegrationOutboxMessage>()
            .AsNoTracking()
            .Where(m => m.Status == IntegrationOutboxMessageStatus.DeadLetter)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPendingReprocessForOriginalEventAsync(
        Guid originalEventId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pending = await context.Set<IntegrationOutboxMessage>()
            .AsNoTracking()
            .Where(m => m.Status == IntegrationOutboxMessageStatus.Pending
                        || m.Status == IntegrationOutboxMessageStatus.InProgress)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var line in pending)
        {
            var envelope = BuildingBlocks.Application.Messaging.IntegrationEventEnvelopeSerializer.Deserialize(
                line.PayloadJson,
                IntegrationEventScope.Catalog);
            if (envelope.IsSuccess && envelope.Value.CorrelationId == originalEventId)
            {
                return true;
            }
        }

        return false;
    }
}