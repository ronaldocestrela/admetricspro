using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;

/// <summary>
/// Manipulador responsável pela remoção de domínio CNAME configurado para o inquilino.
/// </summary>
public sealed class RemoveTenantCustomDomainCommandHandler : ICommandHandler<RemoveTenantCustomDomainCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RemoveTenantCustomDomainCommandHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de inquilinos do MasterDb.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public RemoveTenantCustomDomainCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveTenantCustomDomainCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(command.TenantId), cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Inquilino não localizado no catálogo central."));
        }

        var updateResult = tenant.UpdateBranding(tenant.PrimaryColor, tenant.SecondaryColor, customDomain: null);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        _tenantRepository.Update(tenant);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
