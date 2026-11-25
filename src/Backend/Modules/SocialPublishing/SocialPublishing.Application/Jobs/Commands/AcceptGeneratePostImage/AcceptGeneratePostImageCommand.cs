using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Jobs.Commands.AcceptGeneratePostImage;

/// <summary>
/// Aceita geração assíncrona de imagem (sem chamada ao provedor no request HTTP).
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="PostId">Identificador do post orgânico aprovado.</param>
/// <param name="Direction">Direção criativa opcional para o prompt de imagem.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record AcceptGeneratePostImageCommand(
    Guid WorkspaceId,
    Guid PostId,
    string? Direction) : ICommand<UserInitiatedJobDto>;