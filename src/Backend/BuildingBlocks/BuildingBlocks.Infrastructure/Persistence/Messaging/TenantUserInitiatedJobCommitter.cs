using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Grava job e outbox no banco operacional do tenant em um único commit.
/// </summary>
public sealed class TenantUserInitiatedJobCommitter(ITenantDbContextAccessor accessor) : ITenantUserInitiatedJobCommitter
{
    /// <inheritdoc />
    public async Task<Result> CommitAcceptedJobAsync(
        UserInitiatedJob job,
        IntegrationOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await accessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result.Failure(contextResult.Error);
        }

        var context = contextResult.Value;
        var jobStore = new DbUserInitiatedJobStore(context);
        var jobAdd = await jobStore.AddAsync(job, cancellationToken);
        if (jobAdd.IsFailure)
        {
            return jobAdd;
        }

        var outboxWriter = new DbIntegrationOutboxWriter(context);
        var outboxAdd = await outboxWriter.AddAsync(outboxMessage, cancellationToken);
        if (outboxAdd.IsFailure)
        {
            return outboxAdd;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}