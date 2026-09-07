using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Squads.Commands.AssignSquadWorkspace;

/// <summary>
/// Manipulador responsável por validar e associar um cliente/workspace à carteira de um squad.
/// </summary>
public sealed class AssignSquadWorkspaceCommandHandler : ICommandHandler<AssignSquadWorkspaceCommand>
{
    private readonly ISquadRepository _squadRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AssignSquadWorkspaceCommandHandler"/>.
    /// </summary>
    /// <param name="squadRepository">Repositório de squads.</param>
    /// <param name="workspaceRepository">Repositório de clientes/workspaces.</param>
    /// <param name="unitOfWork">Unidade de trabalho do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    public AssignSquadWorkspaceCommandHandler(
        ISquadRepository squadRepository,
        IWorkspaceRepository workspaceRepository,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AssignSquadWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue || tenantContext.TenantId.Value == Guid.Empty)
        {
            return Result.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Acesso não autorizado: contexto do inquilino não identificado."));
        }

        var squad = await _squadRepository.GetByIdAsync(request.SquadId, cancellationToken);
        if (squad is null)
        {
            return Result.Failure(
                Error.NotFound("Squad.NotFound", $"Squad com identificador '{request.SquadId}' não encontrado."));
        }

        var workspace = await _workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(
                Error.NotFound("Workspace.NotFound", $"Cliente/workspace com identificador '{request.WorkspaceId}' não encontrado no inquilino."));
        }

        if (!workspace.IsActive)
        {
            return Result.Failure(
                Error.Validation("Workspace.Inactive", "Não é permitido alocar workspaces pausados/inativos à carteira de um squad."));
        }

        var assignResult = squad.AssignWorkspace(request.WorkspaceId);
        if (assignResult.IsFailure)
        {
            return assignResult;
        }

        _squadRepository.Update(squad);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
