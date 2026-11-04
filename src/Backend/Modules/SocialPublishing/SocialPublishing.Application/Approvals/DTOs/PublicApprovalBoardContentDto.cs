namespace SocialPublishing.Application.Approvals.DTOs;

/// <summary>
/// Conteúdo do quadro de aprovação retornado pelo módulo Social (sem branding).
/// </summary>
/// <param name="WorkspaceId">Workspace cujos posts pendentes são exibidos.</param>
/// <param name="Posts">Posts em <c>PendingApproval</c>.</param>
public sealed record PublicApprovalBoardContentDto(
    Guid WorkspaceId,
    IReadOnlyList<PublicApprovalPostDto> Posts);
