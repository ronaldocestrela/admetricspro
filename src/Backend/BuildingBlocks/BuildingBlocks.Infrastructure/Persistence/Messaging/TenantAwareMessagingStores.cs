using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Outbox tenant-scoped via <see cref="ITenantDbContextAccessor"/>.
/// </summary>
public sealed class TenantAwareIntegrationOutboxStore(ITenantDbContextAccessor accessor) : IIntegrationOutboxStore
{
    /// <inheritdoc />
    public async Task<IntegrationOutboxMessage?> TryClaimNextAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        if (store is null)
        {
            return null;
        }

        return await store.TryClaimNextAsync(utcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkPublishedAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        if (store is null)
        {
            return;
        }

        await store.MarkPublishedAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RequeueAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        if (store is null)
        {
            return;
        }

        await store.RequeueAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ReclaimStaleAsync(DateTime utcNow, TimeSpan inProgressTimeout, CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        if (store is null)
        {
            return 0;
        }

        return await store.ReclaimStaleAsync(utcNow, inProgressTimeout, cancellationToken);
    }

    private async Task<DbIntegrationOutboxStore?> CreateStoreAsync(CancellationToken cancellationToken)
    {
        var contextResult = await accessor.GetDbContextAsync(cancellationToken);
        return contextResult.IsSuccess ? new DbIntegrationOutboxStore(contextResult.Value) : null;
    }
}

/// <summary>
/// Writer de outbox tenant-scoped.
/// </summary>
public sealed class TenantAwareIntegrationOutboxWriter(ITenantDbContextAccessor accessor) : IIntegrationOutboxWriter
{
    /// <inheritdoc />
    public async Task<Result> AddAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var contextResult = await accessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result.Failure(contextResult.Error);
        }

        return await new DbIntegrationOutboxWriter(contextResult.Value).AddAsync(message, cancellationToken);
    }
}

/// <summary>
/// Inbox tenant-scoped.
/// </summary>
public sealed class TenantAwareIntegrationInboxStore(ITenantDbContextAccessor accessor) : IIntegrationInboxStore
{
    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        return store is not null && await store.ExistsAsync(eventId, consumerName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> TryRecordAsync(
        Guid eventId,
        string consumerName,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        if (store is null)
        {
            return Result.Failure(Error.Failure("Tenant.ContextMissing", "Contexto de tenant indisponível."));
        }

        return await store.TryRecordAsync(eventId, consumerName, processedAtUtc, cancellationToken);
    }

    private async Task<DbIntegrationInboxStore?> CreateStoreAsync(CancellationToken cancellationToken)
    {
        var contextResult = await accessor.GetDbContextAsync(cancellationToken);
        return contextResult.IsSuccess ? new DbIntegrationInboxStore(contextResult.Value) : null;
    }
}