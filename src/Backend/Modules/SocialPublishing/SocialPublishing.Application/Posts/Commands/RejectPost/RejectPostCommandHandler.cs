using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Posts.DTOs;

namespace SocialPublishing.Application.Posts.Commands.RejectPost;

/// <summary>
/// Processa rejeição interna de post orgânico.
/// </summary>
public sealed class RejectPostCommandHandler : ICommandHandler<RejectPostCommand, OrganicPostDto>
{
    private readonly IOrganicPostRepository _postRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserContext _currentUserContext;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RejectPostCommandHandler"/>.
    /// </summary>
    /// <param name="postRepository">Repositório de posts.</param>
    /// <param name="unitOfWork">Unit of work do módulo.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant.</param>
    /// <param name="currentUserContext">Usuário autenticado.</param>
    public RejectPostCommandHandler(
        IOrganicPostRepository postRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserContext currentUserContext)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    /// <inheritdoc />
    public async Task<Result<OrganicPostDto>> Handle(RejectPostCommand request, CancellationToken cancellationToken)
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
        var transition = post.Reject();
        if (transition.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(transition.Error);
        }

        await _postRepository.UpdateAsync(post, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<OrganicPostDto>.Success(OrganicPostMapping.ToDto(post));
    }
}
