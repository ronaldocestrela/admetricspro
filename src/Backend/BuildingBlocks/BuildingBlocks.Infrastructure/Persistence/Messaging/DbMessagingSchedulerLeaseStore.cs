using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Lease de scheduler persistido no MasterDb.
/// </summary>
public sealed class DbMessagingSchedulerLeaseStore(DbContext context) : IMessagingSchedulerLeaseStore
{
    /// <inheritdoc />
    public async Task<Result> TryAcquireOrRenewAsync(
        string leaseKey,
        string holderInstanceId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await context.Set<MessagingSchedulerLease>()
            .FirstOrDefaultAsync(l => l.LeaseKey == leaseKey, cancellationToken);

        if (existing is null)
        {
            var expiresAt = utcNow.Add(leaseDuration);
            var create = MessagingSchedulerLease.Create(
                Guid.NewGuid(),
                leaseKey,
                holderInstanceId,
                utcNow,
                expiresAt);

            if (create.IsFailure)
            {
                return Result.Failure(create.Error);
            }

            await context.Set<MessagingSchedulerLease>().AddAsync(create.Value, cancellationToken);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                context.Entry(create.Value).State = EntityState.Detached;
                return Result.Failure(MessagingErrors.LeaseNotAcquired);
            }

            return Result.Success();
        }

        if (existing.HolderInstanceId == holderInstanceId)
        {
            var renew = existing.Renew(utcNow.Add(leaseDuration));
            if (renew.IsFailure)
            {
                return Result.Failure(renew.Error);
            }

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        if (existing.ExpiresAtUtc > utcNow)
        {
            return Result.Failure(MessagingErrors.LeaseNotAcquired);
        }

        existing.TakeOver(holderInstanceId, utcNow, utcNow.Add(leaseDuration));
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}