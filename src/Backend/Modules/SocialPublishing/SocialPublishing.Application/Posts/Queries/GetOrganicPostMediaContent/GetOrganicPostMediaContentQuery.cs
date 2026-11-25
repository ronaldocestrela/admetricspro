using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Posts.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Posts.Queries.GetOrganicPostMediaContent;

/// <summary>
/// Lê o binário de uma mídia já anexada a um post orgânico.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="PostId">Identificador do post.</param>
/// <param name="AssetId">Identificador do ativo de mídia.</param>
[RequireTenantPermission(TenantPermission.SocialRead, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record GetOrganicPostMediaContentQuery(
    Guid WorkspaceId,
    Guid PostId,
    Guid AssetId) : IQuery<OrganicPostMediaContentDto>;
