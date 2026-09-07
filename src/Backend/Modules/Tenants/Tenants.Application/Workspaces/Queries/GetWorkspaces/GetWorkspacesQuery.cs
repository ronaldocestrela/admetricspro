using BuildingBlocks.Application.Messaging;
using Tenants.Application.Workspaces.DTOs;

namespace Tenants.Application.Workspaces.Queries.GetWorkspaces;

/// <summary>
/// Consulta para listar os workspaces do inquilino com filtro opcional por status ativo.
/// </summary>
/// <param name="ActiveOnly">Filtro opcional para listar apenas workspaces ativos.</param>
public sealed record GetWorkspacesQuery(bool? ActiveOnly = null) : IQuery<IReadOnlyList<WorkspaceDto>>;
