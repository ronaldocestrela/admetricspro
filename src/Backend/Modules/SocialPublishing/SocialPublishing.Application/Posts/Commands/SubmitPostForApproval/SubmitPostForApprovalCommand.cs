using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Posts.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Posts.Commands.SubmitPostForApproval;

/// <summary>
/// Envia um post orgânico de Draft para aprovação interna.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="PostId">Identificador do post.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record SubmitPostForApprovalCommand(Guid WorkspaceId, Guid PostId) : ICommand<OrganicPostDto>;
