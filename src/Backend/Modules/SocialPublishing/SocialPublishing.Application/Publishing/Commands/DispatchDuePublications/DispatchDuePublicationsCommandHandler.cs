using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Publishing;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;
using SocialPublishing.Domain.Publishing;

namespace SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;

/// <summary>
/// Reivindica jobs devidos, publica na rede e persiste transições de post e outbox por job.
/// </summary>
public sealed class DispatchDuePublicationsCommandHandler
    : ICommandHandler<DispatchDuePublicationsCommand, DispatchDuePublicationsSummary>
{
    private readonly IPublicationJobRepository _publicationJobRepository;
    private readonly IOrganicPostRepository _postRepository;
    private readonly IOrganicConnectionRepository _connectionRepository;
    private readonly IOrganicTokenProtector _tokenProtector;
    private readonly IOrganicMediaPublicUrlResolver _mediaPublicUrlResolver;
    private readonly IReadOnlyDictionary<OrganicPlatform, IOrganicPlatformAdapter> _adaptersByPlatform;
    private readonly OrganicPublishRateLimitPolicy _rateLimitPolicy;
    private readonly IOrganicPublishJitterProvider _jitterProvider;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DispatchDuePublicationsCommandHandler"/>.
    /// </summary>
    public DispatchDuePublicationsCommandHandler(
        IPublicationJobRepository publicationJobRepository,
        IOrganicPostRepository postRepository,
        IOrganicConnectionRepository connectionRepository,
        IOrganicTokenProtector tokenProtector,
        IOrganicMediaPublicUrlResolver mediaPublicUrlResolver,
        IEnumerable<IOrganicPlatformAdapter> adapters,
        OrganicPublishRateLimitPolicy rateLimitPolicy,
        IOrganicPublishJitterProvider jitterProvider,
        ISocialPublishingUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOptions<OrganicPublishOptions> publishOptions)
    {
        _ = publishOptions?.Value ?? throw new ArgumentNullException(nameof(publishOptions));
        _publicationJobRepository = publicationJobRepository ?? throw new ArgumentNullException(nameof(publicationJobRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _tokenProtector = tokenProtector ?? throw new ArgumentNullException(nameof(tokenProtector));
        _mediaPublicUrlResolver = mediaPublicUrlResolver ?? throw new ArgumentNullException(nameof(mediaPublicUrlResolver));
        _adaptersByPlatform = (adapters ?? throw new ArgumentNullException(nameof(adapters)))
            .GroupBy(a => a.Platform)
            .ToDictionary(g => g.Key, g => g.First());
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
        _jitterProvider = jitterProvider ?? throw new ArgumentNullException(nameof(jitterProvider));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<DispatchDuePublicationsSummary>> Handle(
        DispatchDuePublicationsCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var due = await _publicationJobRepository.ListDueAsync(utcNow, cancellationToken);
        var succeeded = 0;
        var retried = 0;
        var dead = 0;
        var skipped = 0;

        foreach (var candidate in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = await ProcessJobAsync(candidate.Id, utcNow, cancellationToken);
            switch (outcome)
            {
                case JobProcessOutcome.Succeeded:
                    succeeded++;
                    break;
                case JobProcessOutcome.RetryScheduled:
                    retried++;
                    break;
                case JobProcessOutcome.DeadLetter:
                    dead++;
                    break;
                case JobProcessOutcome.Skipped:
                    skipped++;
                    break;
            }
        }

        return Result<DispatchDuePublicationsSummary>.Success(
            new DispatchDuePublicationsSummary(succeeded, retried, dead, skipped));
    }

    private async Task<JobProcessOutcome> ProcessJobAsync(Guid jobId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var claimed = await _publicationJobRepository.TryClaimAsync(jobId, cancellationToken);
        if (claimed is null)
        {
            return JobProcessOutcome.Skipped;
        }

        try
        {
            var post = await _postRepository.GetByIdAsync(claimed.PostId, cancellationToken);
            if (post is null || post.Status != OrganicPostStatus.Scheduled)
            {
                return JobProcessOutcome.Skipped;
            }

            if (!_rateLimitPolicy.HasAttemptsRemaining(claimed.AttemptCount))
            {
                return await FinalizeDeadLetterAsync(claimed, post, cancellationToken);
            }

            var publishing = post.MarkPublishing();
            if (publishing.IsFailure)
            {
                return JobProcessOutcome.Skipped;
            }

            var publishResult = await PublishAsync(post, cancellationToken);
            if (publishResult.IsSuccess)
            {
                var published = post.MarkPublished(publishResult.Value.ExternalId);
                if (published.IsFailure)
                {
                    return await FinalizeDeadLetterAsync(claimed, post, cancellationToken);
                }

                var succeeded = claimed.MarkSucceeded();
                if (succeeded.IsFailure)
                {
                    return JobProcessOutcome.Skipped;
                }

                await _postRepository.UpdateAsync(post, cancellationToken);
                await _publicationJobRepository.UpdateAsync(claimed, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
                return JobProcessOutcome.Succeeded;
            }

            return await HandlePublishFailureAsync(claimed, post, publishResult.Error, utcNow, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
    }

    private async Task<Result<OrganicPublishReceipt>> PublishAsync(OrganicPost post, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(post.ConnectionId, cancellationToken);
        if (connection is null || connection.Status != OrganicConnectionStatus.Active)
        {
            return Result<OrganicPublishReceipt>.Failure(SocialPublishing.Domain.SocialErrors.ConnectionRequired);
        }

        if (!_adaptersByPlatform.TryGetValue(post.Platform, out var adapter))
        {
            return Result<OrganicPublishReceipt>.Failure(SocialPublishing.Domain.SocialErrors.PlatformUnsupported);
        }

        var purpose = OrganicConnection.EncryptionPurpose(connection.Platform, connection.WorkspaceId, connection.Id);
        var token = _tokenProtector.Unprotect(connection.EncryptedAccessToken, purpose);
        if (token.IsFailure)
        {
            return Result<OrganicPublishReceipt>.Failure(token.Error);
        }

        var mediaItems = new List<OrganicPublishMediaItem>();
        foreach (var asset in post.Media.OrderBy(m => m.SortOrder))
        {
            if (string.IsNullOrWhiteSpace(asset.ObjectKey))
            {
                return Result<OrganicPublishReceipt>.Failure(SocialPublishing.Domain.SocialErrors.PublishFailed);
            }

            var url = _mediaPublicUrlResolver.ResolvePublicUrl(asset.ObjectKey);
            if (url.IsFailure)
            {
                return Result<OrganicPublishReceipt>.Failure(url.Error);
            }

            mediaItems.Add(new OrganicPublishMediaItem(url.Value, asset.SortOrder));
        }

        var request = new OrganicPublishRequest(
            token.Value,
            connection.ExternalAccountId,
            post.Format,
            post.Caption,
            mediaItems);

        return await adapter.PublishAsync(request, cancellationToken);
    }

    private async Task<JobProcessOutcome> HandlePublishFailureAsync(
        PublicationJob job,
        OrganicPost post,
        Error error,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var nextAttemptCount = job.AttemptCount + 1;
        if (!_rateLimitPolicy.HasAttemptsRemaining(nextAttemptCount))
        {
            return await FinalizeDeadLetterAsync(job, post, cancellationToken);
        }

        var delayMs = _rateLimitPolicy.CalculateDelayMs(job.AttemptCount, _jitterProvider.GetJitterMilliseconds());
        if (error is PublishRateLimitedError rateLimited && rateLimited.RetryAfter is { } retryAfter)
        {
            var retryMs = (int)Math.Min(retryAfter.TotalMilliseconds, int.MaxValue);
            delayMs = Math.Max(delayMs, retryMs);
        }

        var nextAttemptAt = utcNow.AddMilliseconds(delayMs);
        var retry = job.ScheduleRetry(nextAttemptAt, nextAttemptCount);
        if (retry.IsFailure)
        {
            return await FinalizeDeadLetterAsync(job, post, cancellationToken);
        }

        var revert = post.ReturnToScheduled();
        if (revert.IsFailure)
        {
            return await FinalizeDeadLetterAsync(job, post, cancellationToken);
        }

        await _postRepository.UpdateAsync(post, cancellationToken);
        await _publicationJobRepository.UpdateAsync(job, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return JobProcessOutcome.RetryScheduled;
    }

    private async Task<JobProcessOutcome> FinalizeDeadLetterAsync(
        PublicationJob job,
        OrganicPost post,
        CancellationToken cancellationToken)
    {
        var dead = job.MarkDeadLetter();
        if (dead.IsFailure && job.Status != PublicationJobStatus.DeadLetter)
        {
            return JobProcessOutcome.Skipped;
        }

        _ = post.MarkDispatchExhausted();

        await _postRepository.UpdateAsync(post, cancellationToken);
        await _publicationJobRepository.UpdateAsync(job, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return JobProcessOutcome.DeadLetter;
    }

    private enum JobProcessOutcome
    {
        Succeeded,
        RetryScheduled,
        DeadLetter,
        Skipped,
    }
}
