using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Publications;

namespace SocialPublishing.Application.Publishing.Commands.ReleasePublicationClaim;

/// <summary>
/// Devolve um job reivindicado à fila de retentativa sem incrementar tentativas.
/// </summary>
public sealed class ReleasePublicationClaimCommandHandler(
    IPublicationJobRepository publicationJobRepository,
    ISocialPublishingUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<ReleasePublicationClaimCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReleasePublicationClaimCommand request, CancellationToken cancellationToken)
    {
        if (request.ClaimTokenUtc.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(SocialPublishing.Domain.SocialErrors.ScheduleInstantNotUtc);
        }

        var job = await publicationJobRepository.GetByIdAsync(request.PublicationJobId, cancellationToken);
        if (job is null
            || job.Status != PublicationJobStatus.InProgress
            || job.ClaimedAtUtc != request.ClaimTokenUtc)
        {
            return Result.Success();
        }

        var nextAttempt = timeProvider.GetUtcNow().UtcDateTime;
        var released = job.ReleaseClaim(nextAttempt);
        if (released.IsFailure)
        {
            return released;
        }

        await publicationJobRepository.UpdateAsync(job, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}