using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;

namespace SocialPublishing.Application.Posts.Commands.ReschedulePost;

/// <summary>
/// Processa reagendamento auditável de post orgânico.
/// </summary>
public sealed class ReschedulePostCommandHandler : ICommandHandler<ReschedulePostCommand, OrganicPostDto>
{
    private readonly IOrganicPostRepository _postRepository;
    private readonly IPublicationJobRepository _publicationJobRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReschedulePostCommandHandler"/>.
    /// </summary>
    /// <param name="postRepository">Repositório de posts.</param>
    /// <param name="publicationJobRepository">Repositório da outbox.</param>
    /// <param name="unitOfWork">Unit of work do módulo.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant.</param>
    /// <param name="currentUserContext">Usuário autenticado.</param>
    /// <param name="timeProvider">Relógio UTC da aplicação.</param>
    public ReschedulePostCommandHandler(
        IOrganicPostRepository postRepository,
        IPublicationJobRepository publicationJobRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _publicationJobRepository = publicationJobRepository ?? throw new ArgumentNullException(nameof(publicationJobRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<OrganicPostDto>> Handle(ReschedulePostCommand request, CancellationToken cancellationToken)
    {
        var loadResult = await OrganicPostCommandSupport.LoadPostAsync(
            request.WorkspaceId,
            request.PostId,
            _postRepository,
            _tenantContextAccessor,
            _currentUserContext,
            cancellationToken);
        if (loadResult.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(loadResult.Error);
        }

        var post = loadResult.Value;
        var statusBefore = post.Status;
        var revisedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (statusBefore == OrganicPostStatus.Scheduled)
        {
            var activeJob = await _publicationJobRepository.GetActiveByPostIdAsync(post.Id, cancellationToken);
            if (activeJob is not null && activeJob.Status == PublicationJobStatus.InProgress)
            {
                return Result<OrganicPostDto>.Failure(SocialErrors.PublicationJobInProgress);
            }
        }

        var reschedule = post.Reschedule(
            request.ScheduledAtUtc,
            _currentUserContext.UserId!.Value,
            revisedAtUtc);
        if (reschedule.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(reschedule.Error);
        }

        if (statusBefore == OrganicPostStatus.Scheduled)
        {
            var jobToCancel = await _publicationJobRepository.GetActiveByPostIdAsync(post.Id, cancellationToken);
            if (jobToCancel is not null &&
                jobToCancel.Status is PublicationJobStatus.Pending or PublicationJobStatus.RetryScheduled)
            {
                var cancel = jobToCancel.Cancel();
                if (cancel.IsFailure)
                {
                    return Result<OrganicPostDto>.Failure(cancel.Error);
                }

                await _publicationJobRepository.UpdateAsync(jobToCancel, cancellationToken);
            }

            var openJob = PublicationJob.Open(post.Id, post.WorkspaceId, post.ScheduledAtUtc, revisedAtUtc);
            if (openJob.IsFailure)
            {
                return Result<OrganicPostDto>.Failure(openJob.Error);
            }

            var addJob = await _publicationJobRepository.AddAsync(openJob.Value, cancellationToken);
            if (addJob.IsFailure)
            {
                return Result<OrganicPostDto>.Failure(addJob.Error);
            }
        }

        await _postRepository.UpdateAsync(post, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<OrganicPostDto>.Success(OrganicPostMapping.ToDto(post));
    }
}
