using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Approvals;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Approvals.Commands.DecidePublicApproval;

/// <summary>
/// Aplica aprovação ou solicitação de alterações nos posts selecionados após validar o token.
/// </summary>
public sealed class DecidePublicApprovalCommandHandler : ICommandHandler<DecidePublicApprovalCommand>
{
    private readonly IApprovalLinkRepository _approvalLinkRepository;
    private readonly IOrganicPostRepository _postRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DecidePublicApprovalCommandHandler"/>.
    /// </summary>
    public DecidePublicApprovalCommandHandler(
        IApprovalLinkRepository approvalLinkRepository,
        IOrganicPostRepository postRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        _approvalLinkRepository = approvalLinkRepository ?? throw new ArgumentNullException(nameof(approvalLinkRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DecidePublicApprovalCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContextAccessor.TenantContext is null || !_tenantContextAccessor.TenantContext.IsResolved)
        {
            return Result.Failure(SocialErrors.TenantRequired);
        }

        if (request.PostIds is null || request.PostIds.Count == 0)
        {
            return Result.Failure(SocialErrors.ApprovalPostSelectionRequired);
        }

        var tokenHash = ApprovalTokenHasher.TryHashPlainToken(request.PlainToken, out _);
        if (tokenHash is null)
        {
            return Result.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        var link = await _approvalLinkRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (link is null || !link.IsActiveAt(_timeProvider.GetUtcNow().UtcDateTime))
        {
            return Result.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        if (request.Decision == PublicApprovalDecision.RequestChanges &&
            string.IsNullOrWhiteSpace(request.Comment))
        {
            return Result.Failure(SocialErrors.ApprovalCommentRequired);
        }

        foreach (var postId in request.PostIds.Distinct())
        {
            if (postId == Guid.Empty)
            {
                continue;
            }

            var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
            if (post is null || post.WorkspaceId != link.WorkspaceId)
            {
                return Result.Failure(SocialErrors.PostNotFound);
            }

            if (post.Status != OrganicPostStatus.PendingApproval)
            {
                return Result.Failure(SocialErrors.InvalidTransition);
            }

            var transition = request.Decision switch
            {
                PublicApprovalDecision.Approve => post.Approve(),
                PublicApprovalDecision.RequestChanges => post.RequestChanges(request.Comment),
                _ => Result.Failure(Error.Validation("Social.ApprovalDecisionInvalid", "Decisão de aprovação inválida.")),
            };

            if (transition.IsFailure)
            {
                return transition;
            }

            await _postRepository.UpdateAsync(post, cancellationToken);
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
