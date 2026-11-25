using BuildingBlocks.Application.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Messaging;
using SocialPublishing.Domain.Publications;
using SocialPublishing.Infrastructure.Persistence;

namespace SocialPublishing.Infrastructure.Publishing;

/// <summary>
/// Persiste claim e outbox de publicação orgânica em um único commit SQL.
/// </summary>
public sealed class OrganicSlotForwarder(
    SocialPublishingDbContextAccessor contextAccessor,
    ITenantContextAccessor tenantContextAccessor,
    IPublicationJobRepository publicationJobRepository,
    TimeProvider timeProvider) : IOrganicSlotForwarder
{
    /// <inheritdoc />
    public async Task<int> ForwardAllDueAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId.Value == Guid.Empty)
        {
            return 0;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var due = await publicationJobRepository.ListDueAsync(utcNow, cancellationToken);
        var forwarded = 0;

        foreach (var candidate in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await TryForwardJobAsync(tenantId.Value, candidate.Id, utcNow, cancellationToken))
            {
                forwarded++;
            }
        }

        return forwarded;
    }

    private async Task<bool> TryForwardJobAsync(
        Guid tenantId,
        Guid jobId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var contextResult = await contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return false;
        }

        var context = contextResult.Value;
        var job = await context.PublicationJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job is null || !job.CanBeClaimed)
        {
            return false;
        }

        var outboxResult = PublishOrganicSlotOutboxFactory.Create(
            tenantId,
            job.Id,
            job.IdempotencyKey,
            utcNow,
            utcNow);

        if (outboxResult.IsFailure)
        {
            return false;
        }

        if (job.MarkInProgress(utcNow).IsFailure)
        {
            return false;
        }

        await context.IntegrationOutboxMessages.AddAsync(outboxResult.Value, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException ex) when (IsDuplicateEventId(ex))
        {
            return false;
        }
    }

    private static bool IsDuplicateEventId(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IntegrationOutboxMessages", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("EventId", StringComparison.OrdinalIgnoreCase) == true;
}