using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Approvals;

namespace SocialPublishing.Application.Approvals.Commands.CreateApprovalLink;

/// <summary>
/// Processa emissão de link de aprovação com token opaco entregue uma única vez.
/// </summary>
public sealed class CreateApprovalLinkCommandHandler : ICommandHandler<CreateApprovalLinkCommand, ApprovalLinkCreatedDto>
{
    private readonly IApprovalLinkRepository _approvalLinkRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreateApprovalLinkCommandHandler"/>.
    /// </summary>
    public CreateApprovalLinkCommandHandler(
        IApprovalLinkRepository approvalLinkRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _approvalLinkRepository = approvalLinkRepository ?? throw new ArgumentNullException(nameof(approvalLinkRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<ApprovalLinkCreatedDto>> Handle(
        CreateApprovalLinkCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<ApprovalLinkCreatedDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (_tenantContextAccessor.TenantContext is null || !_tenantContextAccessor.TenantContext.IsResolved)
        {
            return Result<ApprovalLinkCreatedDto>.Failure(SocialErrors.TenantRequired);
        }

        if (!_currentUserContext.IsAuthenticated || _currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result<ApprovalLinkCreatedDto>.Failure(SocialErrors.UserRequired);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (request.ExpiresAtUtc <= now)
        {
            return Result<ApprovalLinkCreatedDto>.Failure(SocialErrors.ApprovalLinkExpirationInvalid);
        }

        await _approvalLinkRepository.RevokeActiveByWorkspaceAsync(request.WorkspaceId, now, cancellationToken);

        var plainToken = ApprovalTokenHasher.GeneratePlainToken(out var tokenBytes);
        var tokenHash = ApprovalTokenHasher.HashBytes(tokenBytes);

        var createResult = ApprovalLink.Create(
            request.WorkspaceId,
            tokenHash,
            request.ExpiresAtUtc,
            now);
        if (createResult.IsFailure)
        {
            return Result<ApprovalLinkCreatedDto>.Failure(createResult.Error);
        }

        await _approvalLinkRepository.AddAsync(createResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var relativePath = $"/approve/{plainToken}";
        return Result<ApprovalLinkCreatedDto>.Success(new ApprovalLinkCreatedDto(
            plainToken,
            DateTime.SpecifyKind(request.ExpiresAtUtc, DateTimeKind.Utc),
            relativePath));
    }
}
