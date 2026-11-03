using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Brand.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Brand.Queries.GetBrandVoice;

/// <summary>
/// Consulta o DNA de marca persistido para um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace cliente.</param>
[RequireTenantPermission(TenantPermission.SocialRead, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record GetBrandVoiceQuery(Guid WorkspaceId) : IQuery<BrandVoiceDto>;
