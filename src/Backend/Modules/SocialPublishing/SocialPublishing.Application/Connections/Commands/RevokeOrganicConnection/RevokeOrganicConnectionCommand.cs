using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Connections.Commands.RevokeOrganicConnection;

/// <summary>
/// Revoga localmente uma conexão orgânica e invalida o ciphertext persistido.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário da conexão.</param>
/// <param name="ConnectionId">Identificador da conexão a revogar.</param>
[RequireTenantPermission(TenantPermission.SocialConnectionsManage, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record RevokeOrganicConnectionCommand(Guid WorkspaceId, Guid ConnectionId) : ICommand;
