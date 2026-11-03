using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain.Publications;

namespace SocialPublishing.Application.Posts.Commands.SchedulePost;

/// <summary>
/// Processa agendamento interno de post aprovado e materializa job outbox.
/// </summary>
public sealed class SchedulePostCommandHandler : ICommandHandler<SchedulePostCommand, OrganicPostDto>
{
    private readonly IOrganicPostRepository _postRepository;
    private readonly IPublicationJobRepository _publicationJobRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SchedulePostCommandHandler"/>.
    /// </summary>
    /// <param name="postRepository">Repositório de posts.</param>
    /// <param name="publicationJobRepository">Repositório da outbox.</param>
    /// <param name="unitOfWork">Unit of work do módulo.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant.</param>
    /// <param name="currentUserContext">Usuário autenticado.</param>
    /// <param name="timeProvider">Relógio UTC da aplicação.</param>
    public SchedulePostCommandHandler(
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
    public async Task<Result<OrganicPostDto>> Handle(SchedulePostCommand request, CancellationToken cancellationToken)
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
        var transition = post.Schedule();
        if (transition.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(transition.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var openJob = PublicationJob.Open(post.Id, post.WorkspaceId, post.ScheduledAtUtc, nowUtc);
        if (openJob.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(openJob.Error);
        }

        var addJob = await _publicationJobRepository.AddAsync(openJob.Value, cancellationToken);
        if (addJob.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(addJob.Error);
        }

        await _postRepository.UpdateAsync(post, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<OrganicPostDto>.Success(OrganicPostMapping.ToDto(post));
    }
}
