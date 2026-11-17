using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Posts.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Posts.Commands.ReschedulePost;

/// <summary>
/// Reagenda um post orgânico para um novo instante UTC.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="PostId">Identificador do post.</param>
/// <param name="ScheduledAtUtc">Novo horário em UTC.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record ReschedulePostCommand(
    Guid WorkspaceId,
    Guid PostId,
    DateTime ScheduledAtUtc) : ICommand<OrganicPostDto>;
