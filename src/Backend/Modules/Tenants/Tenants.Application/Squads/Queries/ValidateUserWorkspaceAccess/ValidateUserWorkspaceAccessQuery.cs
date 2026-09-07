using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Queries.ValidateUserWorkspaceAccess;

/// <summary>
/// Consulta pontual para validar se um usuário tem permissão de acesso a um determinado workspace.
/// </summary>
/// <param name="UserId">Identificador do colaborador.</param>
/// <param name="WorkspaceId">Identificador do cliente/workspace.</param>
public sealed record ValidateUserWorkspaceAccessQuery(Guid UserId, Guid WorkspaceId) : IQuery<bool>;
