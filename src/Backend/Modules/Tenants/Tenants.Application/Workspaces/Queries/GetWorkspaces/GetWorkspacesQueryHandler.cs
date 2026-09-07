using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Workspaces.DTOs;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Workspaces.Queries.GetWorkspaces;

/// <summary>
/// Manipulador da consulta <see cref="GetWorkspacesQuery"/> que retorna os workspaces projetados para DTOs.
/// </summary>
public sealed class GetWorkspacesQueryHandler : IQueryHandler<GetWorkspacesQuery, IReadOnlyList<WorkspaceDto>>
{
    private readonly IWorkspaceRepository _workspaceRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetWorkspacesQueryHandler"/>.
    /// </summary>
    /// <param name="workspaceRepository">Repositório de workspaces.</param>
    public GetWorkspacesQueryHandler(IWorkspaceRepository workspaceRepository)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkspaceDto>>> Handle(GetWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var workspaces = await _workspaceRepository.GetAllAsync(request.ActiveOnly, cancellationToken);

        var dtos = workspaces.Select(w => new WorkspaceDto(
            w.Id,
            w.Name,
            w.CnpjOrCpf,
            w.MonthlyAdSpendBudget,
            w.Segment,
            w.IsActive,
            w.CreatedAtUtc,
            w.UpdatedAtUtc)).ToList();

        return Result<IReadOnlyList<WorkspaceDto>>.Success(dtos);
    }
}
