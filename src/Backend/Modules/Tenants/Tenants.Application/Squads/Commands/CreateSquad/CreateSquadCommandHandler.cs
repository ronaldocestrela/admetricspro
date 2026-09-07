using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Repositories;

namespace Tenants.Application.Squads.Commands.CreateSquad;

/// <summary>
/// Manipulador responsável pela validação e persistência na criação de um novo <see cref="Squad"/>.
/// </summary>
public sealed class CreateSquadCommandHandler : ICommandHandler<CreateSquadCommand, Guid>
{
    private readonly ISquadRepository _squadRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreateSquadCommandHandler"/>.
    /// </summary>
    /// <param name="squadRepository">Repositório operacional de squads.</param>
    /// <param name="unitOfWork">Unidade de trabalho do banco dedicado do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    public CreateSquadCommandHandler(
        ISquadRepository squadRepository,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateSquadCommand request, CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue || tenantContext.TenantId.Value == Guid.Empty)
        {
            return Result<Guid>.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Acesso não autorizado: contexto do inquilino não identificado."));
        }

        var squadResult = Squad.Create(Guid.NewGuid(), request.Name, request.Description);
        if (squadResult.IsFailure)
        {
            return Result<Guid>.Failure(squadResult.Error);
        }

        var nameExists = await _squadRepository.ExistsByNameAsync(squadResult.Value.Name, cancellationToken: cancellationToken);
        if (nameExists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Squad.NameAlreadyExists", $"Já existe um squad cadastrado com o nome '{squadResult.Value.Name}' neste inquilino."));
        }

        await _squadRepository.AddAsync(squadResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(squadResult.Value.Id);
    }
}
