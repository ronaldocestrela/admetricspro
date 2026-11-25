using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain.Publications;
using SocialPublishing.Infrastructure.Persistence;
namespace SocialPublishing.Infrastructure.Publishing;

/// <summary>
/// Detecta jobs orgânicos com claim expirado e os devolve a <see cref="PublicationJobStatus.RetryScheduled"/>.
/// </summary>
public sealed class OrganicPublicationClaimReclaimer(
    SocialPublishingDbContextAccessor contextAccessor,
    IOptions<SocialPublishingDispatchOptions> dispatchOptions,
    TimeProvider timeProvider) : IOrganicPublicationClaimReclaimer
{
    /// <inheritdoc />
    public async Task<int> ReclaimStaleClaimsAsync(CancellationToken cancellationToken = default)
    {
        var contextResult = await contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return 0;
        }

        var timeoutSeconds = Math.Max(60, dispatchOptions.Value.InProgressTimeoutSeconds);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var staleBefore = utcNow.AddSeconds(-timeoutSeconds);

        var context = contextResult.Value;
        var staleJobs = await context.PublicationJobs
            .Where(j =>
                j.Status == PublicationJobStatus.InProgress &&
                j.ClaimedAtUtc != null &&
                j.ClaimedAtUtc <= staleBefore)
            .ToListAsync(cancellationToken);

        if (staleJobs.Count == 0)
        {
            return 0;
        }

        var reclaimed = 0;
        foreach (var job in staleJobs)
        {
            if (job.ReleaseClaim(utcNow).IsSuccess)
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
}