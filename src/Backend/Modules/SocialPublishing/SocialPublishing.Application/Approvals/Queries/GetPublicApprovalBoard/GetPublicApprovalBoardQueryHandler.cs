using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Approvals;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Approvals.Queries.GetPublicApprovalBoard;

/// <summary>
/// Carrega posts <see cref="OrganicPostStatus.PendingApproval"/> do workspace do link válido (sem branding).
/// </summary>
public sealed class GetPublicApprovalBoardQueryHandler : IQueryHandler<GetPublicApprovalBoardQuery, PublicApprovalBoardContentDto>
{
    private readonly IApprovalLinkRepository _approvalLinkRepository;
    private readonly IOrganicPostRepository _postRepository;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetPublicApprovalBoardQueryHandler"/>.
    /// </summary>
    public GetPublicApprovalBoardQueryHandler(
        IApprovalLinkRepository approvalLinkRepository,
        IOrganicPostRepository postRepository,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        _approvalLinkRepository = approvalLinkRepository ?? throw new ArgumentNullException(nameof(approvalLinkRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<PublicApprovalBoardContentDto>> Handle(
        GetPublicApprovalBoardQuery request,
        CancellationToken cancellationToken)
    {
        if (_tenantContextAccessor.TenantContext is null || !_tenantContextAccessor.TenantContext.IsResolved)
        {
            return Result<PublicApprovalBoardContentDto>.Failure(SocialErrors.TenantRequired);
        }

        var tokenHash = ApprovalTokenHasher.TryHashPlainToken(request.PlainToken, out _);
        if (tokenHash is null)
        {
            return Result<PublicApprovalBoardContentDto>.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        var link = await _approvalLinkRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (link is null || !link.IsActiveAt(_timeProvider.GetUtcNow().UtcDateTime))
        {
            return Result<PublicApprovalBoardContentDto>.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        var posts = await _postRepository.ListByWorkspaceAndStatusAsync(
            link.WorkspaceId,
            OrganicPostStatus.PendingApproval,
            cancellationToken);

        var dtos = posts
            .OrderBy(p => p.ScheduledAtUtc)
            .Select(p => new PublicApprovalPostDto(
                p.Id,
                p.Caption,
                p.Platform,
                p.Format,
                p.Pillar,
                p.ScheduledAtUtc))
            .ToList();

        return Result<PublicApprovalBoardContentDto>.Success(
            new PublicApprovalBoardContentDto(link.WorkspaceId, dtos));
    }
}
