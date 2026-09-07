using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.Services;
using Tenants.Application.Workspaces.DTOs;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Squads.Queries.GetUserAccessibleWorkspaces;

/// <summary>
/// Manipulador responsável por consultar os clientes acessíveis a um usuário conforme a governança de carteira.
/// </summary>
public sealed class GetUserAccessibleWorkspacesQueryHandler : IQueryHandler<GetUserAccessibleWorkspacesQuery, IReadOnlyList<WorkspaceDto>>
{
    private readonly IUserPortfolioService _portfolioService;
    private readonly IWorkspaceRepository _workspaceRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetUserAccessibleWorkspacesQueryHandler"/>.
    /// </summary>
    /// <param name="portfolioService">Serviço de governança e isolamento por carteira.</param>
    /// <param name="workspaceRepository">Repositório de workspaces.</param>
    public GetUserAccessibleWorkspacesQueryHandler(
        IUserPortfolioService portfolioService,
        IWorkspaceRepository workspaceRepository)
    {
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkspaceDto>>> Handle(GetUserAccessibleWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var accessibleIdsResult = await _portfolioService.GetAccessibleWorkspaceIdsAsync(request.UserId, cancellationToken);
        if (accessibleIdsResult.IsFailure)
        {
            return Result<IReadOnlyList<WorkspaceDto>>.Failure(accessibleIdsResult.Error);
        }

        var accessibleIds = accessibleIdsResult.Value.ToHashSet();
        if (accessibleIds.Count == 0)
        {
            return Result<IReadOnlyList<WorkspaceDto>>.Success(Array.Empty<WorkspaceDto>());
        }

        var allWorkspaces = await _workspaceRepository.GetAllAsync(cancellationToken: cancellationToken);
        var filteredDtos = allWorkspaces
            .Where(w => accessibleIds.Contains(w.Id))
            .Select(w => new WorkspaceDto(
                w.Id,
                w.Name,
                w.CnpjOrCpf,
                w.MonthlyAdSpendBudget,
                w.Segment,
                w.IsActive,
                w.CreatedAtUtc,
                w.UpdatedAtUtc))
            .ToList();

        return Result<IReadOnlyList<WorkspaceDto>>.Success(filteredDtos);
    }
}
