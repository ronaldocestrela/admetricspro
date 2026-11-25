using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Jobs.Queries.GetUserInitiatedJob;

/// <summary>
/// Consulta o status de um job iniciado pelo usuário.
/// </summary>
/// <param name="WorkspaceId">Workspace de escopo.</param>
/// <param name="JobId">Identificador do job.</param>
public sealed record GetUserInitiatedJobQuery(Guid WorkspaceId, Guid JobId) : IQuery<UserInitiatedJobDto>;