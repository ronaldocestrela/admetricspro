using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Connections.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Connections.Queries.ListOrganicConnections;

/// <summary>
/// Lista conexões orgânicas de um workspace sem expor segredos.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário das conexões.</param>
[RequireTenantPermission(TenantPermission.SocialRead, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record ListOrganicConnectionsQuery(Guid WorkspaceId)
    : IQuery<IReadOnlyList<OrganicConnectionSummaryDto>>;
