using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Repositories;

namespace Tenants.Application.Squads.Commands.ToggleSquadStatus;

/// <summary>
/// Manipulador responsável por alternar o status operacional de um <see cref="BuildingBlocks.Domain.Tenants.Squad"/>.
/// </summary>
public sealed class ToggleSquadStatusCommandHandler : ICommandHandler<ToggleSquadStatusCommand>
{
    private readonly ISquadRepository _squadRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ToggleSquadStatusCommandHandler"/>.
    /// </summary>
    /// <param name="squadRepository">Repositório de squads.</param>
    /// <param name="unitOfWork">Unidade de trabalho do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    public ToggleSquadStatusCommandHandler(
        ISquadRepository squadRepository,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ToggleSquadStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue || tenantContext.TenantId.Value == Guid.Empty)
        {
            return Result.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Acesso não autorizado: contexto do inquilino não identificado."));
        }

        var squad = await _squadRepository.GetByIdAsync(request.Id, cancellationToken);
        if (squad is null)
        {
            return Result.Failure(
                Error.NotFound("Squad.NotFound", $"Squad com identificador '{request.Id}' não encontrado."));
        }

        squad.ToggleStatus(request.IsActive);
        _squadRepository.Update(squad);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
