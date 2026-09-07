using BuildingBlocks.Application.Messaging;
using Tenants.Application.Workspaces.DTOs;

namespace Tenants.Application.Squads.Queries.GetUserAccessibleWorkspaces;

/// <summary>
/// Consulta para obter a carteira de clientes/workspaces aos quais o usuário especificado possui autorização de acesso.
/// </summary>
/// <param name="UserId">Identificador do colaborador.</param>
public sealed record GetUserAccessibleWorkspacesQuery(Guid UserId) : IQuery<IReadOnlyList<WorkspaceDto>>;
