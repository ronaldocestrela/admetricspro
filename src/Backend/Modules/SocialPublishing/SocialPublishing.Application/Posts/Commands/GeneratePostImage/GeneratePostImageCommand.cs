using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Posts.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Posts.Commands.GeneratePostImage;

/// <summary>
/// Gera imagem de feed para um post aprovado via Gemini Nano Banana.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="PostId">Identificador do post.</param>
/// <param name="Direction">Direcionamento visual opcional da agência (até 500 caracteres).</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record GeneratePostImageCommand(
    Guid WorkspaceId,
    Guid PostId,
    string? Direction = null) : ICommand<OrganicPostDto>;