using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using MediatR;
using SocialPublishing.Application.Media.DTOs;
using SocialPublishing.Domain.Posts;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Media.Commands.RegisterOrganicMedia;

/// <summary>
/// Comando para registrar mídia orgânica standalone (metadados no SQL, binário na porta de storage).
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="Format">Formato de publicação alvo.</param>
/// <param name="MimeType">Tipo MIME do arquivo.</param>
/// <param name="Content">Conteúdo do arquivo (não persistido pelo MediatR).</param>
/// <param name="CarouselAnchorAssetId">Identificador da mídia âncora do carrossel, se houver.</param>
[RequireTenantPermission(TenantPermission.SocialMediaManage, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record RegisterOrganicMediaCommand(
    Guid WorkspaceId,
    OrganicPostFormat Format,
    string MimeType,
    Stream Content,
    Guid? CarouselAnchorAssetId = null) : IRequest<Result<OrganicMediaAssetDto>>;
