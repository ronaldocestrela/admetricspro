using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using SocialPublishing.Application.Abstractions;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Grava job e outbox compartilhados no contexto social em um único commit SQL.
/// </summary>
public sealed class SocialUserInitiatedJobCommitter(
    SocialPublishingDbContextAccessor contextAccessor,
    ISocialPublishingUnitOfWork unitOfWork) : ISocialUserInitiatedJobCommitter
{
    /// <inheritdoc />
    public async Task<Result> CommitAcceptedJobAsync(
        UserInitiatedJob job,
        IntegrationOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await contextAccessor.GetDbContextAsync(cancellationToken);
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

        await context.IntegrationOutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}