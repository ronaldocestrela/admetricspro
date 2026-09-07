using BuildingBlocks.Application.Messaging;
using Tenants.Application.Workspaces.DTOs;

namespace Tenants.Application.Workspaces.Queries.GetWorkspaceById;

/// <summary>
/// Consulta para obter detalhes de um workspace pelo seu identificador único.
/// </summary>
/// <param name="Id">Identificador único do workspace.</param>
public sealed record GetWorkspaceByIdQuery(Guid Id) : IQuery<WorkspaceDto>;
