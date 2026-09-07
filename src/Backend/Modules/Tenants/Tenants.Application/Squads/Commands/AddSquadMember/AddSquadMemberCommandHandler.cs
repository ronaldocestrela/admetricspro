using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;

namespace Tenants.Application.Squads.Commands.AddSquadMember;

/// <summary>
/// Manipulador responsável por validar e vincular um colaborador a um squad.
/// </summary>
public sealed class AddSquadMemberCommandHandler : ICommandHandler<AddSquadMemberCommand>
{
    private readonly ISquadRepository _squadRepository;
    private readonly ITenantUserRepository _userRepository;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AddSquadMemberCommandHandler"/>.
    /// </summary>
    /// <param name="squadRepository">Repositório de squads.</param>
    /// <param name="userRepository">Repositório de colaboradores do inquilino.</param>
    /// <param name="unitOfWork">Unidade de trabalho do inquilino.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo.</param>
    public AddSquadMemberCommandHandler(
        ISquadRepository squadRepository,
        ITenantUserRepository userRepository,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AddSquadMemberCommand request, CancellationToken cancellationToken)
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

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(
                Error.NotFound("User.NotFound", $"Colaborador com identificador '{request.UserId}' não encontrado no inquilino."));
        }

        if (!user.IsActive)
        {
            return Result.Failure(
                Error.Validation("User.Inactive", "Não é permitido vincular colaboradores inativos ao squad."));
        }

        var addResult = squad.AddMember(request.UserId);
        if (addResult.IsFailure)
        {
            return addResult;
        }

        _squadRepository.Update(squad);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
