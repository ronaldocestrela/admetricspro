using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Posts.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Posts.Commands.SchedulePost;

/// <summary>
/// Agenda um post aprovado e abre job pendente na outbox.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="PostId">Identificador do post.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record SchedulePostCommand(Guid WorkspaceId, Guid PostId) : ICommand<OrganicPostDto>;
