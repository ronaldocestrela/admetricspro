using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Posts.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Posts.Commands.ReviseOrganicPostCaption;

/// <summary>
/// Atualiza a legenda de um post orgânico em <see cref="SocialPublishing.Domain.Posts.OrganicPostStatus.Draft"/> ou <see cref="SocialPublishing.Domain.Posts.OrganicPostStatus.ChangesRequested"/>.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="PostId">Identificador do post.</param>
/// <param name="Caption">Nova legenda.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record ReviseOrganicPostCaptionCommand(Guid WorkspaceId, Guid PostId, string Caption) : ICommand<OrganicPostDto>;