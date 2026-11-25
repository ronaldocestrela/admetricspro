using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Store EF de outbox com claim otimista por <see cref="IntegrationOutboxMessage.RowVersion"/>.
/// </summary>
public sealed class DbIntegrationOutboxStore(DbContext context) : IIntegrationOutboxStore
{
    /// <inheritdoc />
    public async Task<IntegrationOutboxMessage?> TryClaimNextAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var candidate = await context.Set<IntegrationOutboxMessage>()
            .Where(m => m.Status == IntegrationOutboxMessageStatus.Pending
                        && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= utcNow))
            .OrderBy(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is null)
        {
            return null;
        }

        var claim = candidate.MarkInProgress(utcNow);
        if (claim.IsFailure)
        {
            return null;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return candidate;
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task MarkPublishedAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Set<IntegrationOutboxMessage>().Update(message);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RequeueAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Set<IntegrationOutboxMessage>().Update(message);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ReclaimStaleAsync(DateTime utcNow, TimeSpan inProgressTimeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stale = await context.Set<IntegrationOutboxMessage>()
            .Where(m => m.Status == IntegrationOutboxMessageStatus.InProgress && m.InProgressSinceUtc != null)
            .ToListAsync(cancellationToken);

        var reclaimed = 0;
        foreach (var message in stale)
        {
            if (message.Reclaim(utcNow, inProgressTimeout).IsSuccess)
            {
                reclaimed++;
            }
        }

        if (reclaimed > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return reclaimed;
    }

    /// <inheritdoc />
    public async Task<IntegrationOutboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await context.Set<IntegrationOutboxMessage>()
            .AsNoTracking()
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
            var scope = line.TenantId is null ? IntegrationEventScope.Catalog : IntegrationEventScope.Operational;
            var envelope = BuildingBlocks.Application.Messaging.IntegrationEventEnvelopeSerializer.Deserialize(
                line.PayloadJson,
                scope);
            if (envelope.IsSuccess && envelope.Value.CorrelationId == originalEventId)
            {
                return true;
            }
        }

        return false;
    }
}