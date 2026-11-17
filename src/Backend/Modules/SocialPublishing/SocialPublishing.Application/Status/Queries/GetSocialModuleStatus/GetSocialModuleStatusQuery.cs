using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Status.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Status.Queries.GetSocialModuleStatus;

/// <summary>
/// Consulta o resumo de conexões e posts agendados do módulo Social Publishing para um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace cliente.</param>
[RequireTenantPermission(TenantPermission.SocialRead, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record GetSocialModuleStatusQuery(Guid WorkspaceId) : IQuery<SocialModuleStatusDto>;
