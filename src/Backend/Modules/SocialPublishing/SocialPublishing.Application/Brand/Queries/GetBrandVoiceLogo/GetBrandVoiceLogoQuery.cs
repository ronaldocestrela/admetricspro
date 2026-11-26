using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Brand.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Brand.Queries.GetBrandVoiceLogo;

/// <summary>
/// Obtém o conteúdo da logo do cliente para prévia autenticada.
/// </summary>
/// <param name="WorkspaceId">Workspace do cliente.</param>
[RequireTenantPermission(TenantPermission.SocialRead, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record GetBrandVoiceLogoQuery(Guid WorkspaceId) : IQuery<BrandVoiceLogoContentDto>;