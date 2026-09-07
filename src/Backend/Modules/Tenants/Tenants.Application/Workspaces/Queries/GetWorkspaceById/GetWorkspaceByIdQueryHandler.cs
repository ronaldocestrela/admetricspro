using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Workspaces.DTOs;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Workspaces.Queries.GetWorkspaceById;

/// <summary>
/// Manipulador da consulta <see cref="GetWorkspaceByIdQuery"/> que busca os detalhes de um workspace específico.
/// </summary>
public sealed class GetWorkspaceByIdQueryHandler : IQueryHandler<GetWorkspaceByIdQuery, WorkspaceDto>
{
    private readonly IWorkspaceRepository _workspaceRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetWorkspaceByIdQueryHandler"/>.
    /// </summary>
    /// <param name="workspaceRepository">Repositório de workspaces.</param>
    public GetWorkspaceByIdQueryHandler(IWorkspaceRepository workspaceRepository)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
    }

    /// <inheritdoc />
    public async Task<Result<WorkspaceDto>> Handle(GetWorkspaceByIdQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(request.Id, cancellationToken);
        if (workspace is null)
        {
            return Result<WorkspaceDto>.Failure(
                Error.NotFound("Workspace.NotFound", $"Workspace com identificador '{request.Id}' não encontrado."));
        }

        var dto = new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.CnpjOrCpf,
            workspace.MonthlyAdSpendBudget,
            workspace.Segment,
            workspace.IsActive,
            workspace.CreatedAtUtc,
            workspace.UpdatedAtUtc);

        return Result<WorkspaceDto>.Success(dto);
    }
}
