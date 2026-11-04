using BuildingBlocks.Application.Messaging;
using SocialPublishing.Application.Approvals.DTOs;

namespace SocialPublishing.Application.Approvals.Queries.GetPublicApprovalBoard;

/// <summary>
/// Consulta anônima do quadro de posts pendentes vinculado a um token de aprovação.
/// </summary>
/// <param name="PlainToken">Token opaco recebido na URL.</param>
public sealed record GetPublicApprovalBoardQuery(string PlainToken) : IQuery<PublicApprovalBoardContentDto>;
