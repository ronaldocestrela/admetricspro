using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Repositories;

namespace Tenants.Application.Squads.Commands.UpdateSquad;

/// <summary>
/// Manipulador responsável por atualizar os dados descritivos de um <see cref="BuildingBlocks.Domain.Tenants.Squad"/>.
/// </summary>
public sealed class UpdateSquadCommandHandler : ICommandHandler<UpdateSquadCommand>
{
    private readonly ISquadRepository _squadRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="UpdateSquadCommandHandler"/>.
    /// </summary>
    /// <param name="squadRepository">Repositório de squads.</param>
    /// <param name="unitOfWork">Unidade de trabalho do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    public UpdateSquadCommandHandler(
        ISquadRepository squadRepository,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateSquadCommand request, CancellationToken cancellationToken)
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

        var nameExists = await _squadRepository.ExistsByNameAsync(request.Name, excludeId: request.Id, cancellationToken: cancellationToken);
        if (nameExists)
        {
            return Result.Failure(
                Error.Conflict("Squad.NameAlreadyExists", $"Já existe outro squad cadastrado com o nome '{request.Name.Trim()}' neste inquilino."));
        }

        var updateResult = squad.UpdateDetails(request.Name, request.Description);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        _squadRepository.Update(squad);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
