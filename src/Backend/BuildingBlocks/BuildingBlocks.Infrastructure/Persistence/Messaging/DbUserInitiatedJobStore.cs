using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Store EF de jobs iniciados pelo usuário.
/// </summary>
public sealed class DbUserInitiatedJobStore(DbContext context) : IUserInitiatedJobStore
{
    /// <inheritdoc />
    public async Task<UserInitiatedJob?> GetByIdAsync(
        Guid workspaceId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<UserInitiatedJob>()
            .FirstOrDefaultAsync(j => j.Id == jobId && j.WorkspaceId == workspaceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserInitiatedJob?> GetByIdForExecutionAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<UserInitiatedJob>()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> AddAsync(UserInitiatedJob job, CancellationToken cancellationToken = default)
    {
        var duplicate = await context.Set<UserInitiatedJob>()
            .AsNoTracking()
            .AnyAsync(j => j.EventId == job.EventId, cancellationToken);

        if (duplicate)
        {
            return Result.Failure(Error.Conflict("UserJob.DuplicateEventId", "Já existe job para este EventId."));
        }

        await context.Set<UserInitiatedJob>().AddAsync(job, cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(UserInitiatedJob job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Set<UserInitiatedJob>().Update(job);
        await Task.CompletedTask;
        return Result.Success();
    }
}