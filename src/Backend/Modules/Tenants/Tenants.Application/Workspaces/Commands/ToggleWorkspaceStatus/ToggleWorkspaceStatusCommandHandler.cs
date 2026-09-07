using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Repositories;

namespace Tenants.Application.Workspaces.Commands.ToggleWorkspaceStatus;

/// <summary>
/// Manipulador responsável por alternar o status operacional do workspace respeitando as cotas do plano contratado.
/// </summary>
public sealed class ToggleWorkspaceStatusCommandHandler : ICommandHandler<ToggleWorkspaceStatusCommand>
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ToggleWorkspaceStatusCommandHandler"/>.
    /// </summary>
    /// <param name="workspaceRepository">Repositório de workspaces.</param>
    /// <param name="unitOfWork">Unidade de trabalho do banco de dados do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor de contexto do inquilino ativo.</param>
    /// <param name="sender">Mediador in-memory para consulta de cotas de plano.</param>
    public ToggleWorkspaceStatusCommandHandler(
        IWorkspaceRepository workspaceRepository,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        ISender sender)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ToggleWorkspaceStatusCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(request.Id, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(
                Error.NotFound("Workspace.NotFound", $"Workspace com identificador '{request.Id}' não encontrado."));
        }

        if (workspace.IsActive)
        {
            // Desativação não requer validação de cota adicional
            var deactivateResult = workspace.Deactivate();
            if (deactivateResult.IsFailure)
            {
                return deactivateResult;
            }
        }
        else
        {
            // Reativação requer checagem de cota com o plano do inquilino
            var tenantContext = _tenantContextAccessor.TenantContext;
            if (tenantContext.IsResolved && tenantContext.TenantId.HasValue && tenantContext.TenantId.Value != Guid.Empty)
            {
                var planLimitsResult = await _sender.Send(new GetTenantPlanLimitsQuery(tenantContext.TenantId.Value), cancellationToken);
                if (planLimitsResult.IsFailure)
                {
                    return Result.Failure(planLimitsResult.Error);
                }

                var currentActiveCount = await _workspaceRepository.CountActiveAsync(cancellationToken);
                if (currentActiveCount >= planLimitsResult.Value.MaxWorkspaces)
                {
                    return Result.Failure(
                        Error.Validation("Workspace.QuotaExceeded",
                            $"Não é possível reativar o workspace: limite de clientes/workspaces ativos atingido ({planLimitsResult.Value.MaxWorkspaces})."));
                }
            }

            var activateResult = workspace.Activate();
            if (activateResult.IsFailure)
            {
                return activateResult;
            }
        }

        _workspaceRepository.Update(workspace);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
