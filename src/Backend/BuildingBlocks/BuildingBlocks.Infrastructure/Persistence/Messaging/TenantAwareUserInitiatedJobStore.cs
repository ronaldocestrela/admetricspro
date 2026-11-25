using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Store de jobs tenant-scoped via <see cref="ITenantDbContextAccessor"/>.
/// </summary>
public sealed class TenantAwareUserInitiatedJobStore(ITenantDbContextAccessor accessor) : IUserInitiatedJobStore
{
    /// <inheritdoc />
    public async Task<UserInitiatedJob?> GetByIdAsync(
        Guid workspaceId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        return store is null
            ? null
            : await store.GetByIdAsync(workspaceId, jobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserInitiatedJob?> GetByIdForExecutionAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        return store is null
            ? null
            : await store.GetByIdForExecutionAsync(jobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> AddAsync(UserInitiatedJob job, CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        if (store is null)
        {
            return Result.Failure(Error.Failure("Tenant.ContextMissing", "Contexto de tenant indisponível."));
        }

        return await store.AddAsync(job, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(UserInitiatedJob job, CancellationToken cancellationToken = default)
    {
        var store = await CreateStoreAsync(cancellationToken);
        if (store is null)
        {
            return Result.Failure(Error.Failure("Tenant.ContextMissing", "Contexto de tenant indisponível."));
        }

        return await store.UpdateAsync(job, cancellationToken);
    }

    private async Task<DbUserInitiatedJobStore?> CreateStoreAsync(CancellationToken cancellationToken)
    {
        var contextResult = await accessor.GetDbContextAsync(cancellationToken);
        return contextResult.IsSuccess ? new DbUserInitiatedJobStore(contextResult.Value) : null;
    }
}