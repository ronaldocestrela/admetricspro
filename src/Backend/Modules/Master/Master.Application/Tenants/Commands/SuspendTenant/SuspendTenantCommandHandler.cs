using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.SuspendTenant;

/// <summary>
/// Handles <see cref="SuspendTenantCommand"/> to suspend a tenant's operations.
/// </summary>
public sealed class SuspendTenantCommandHandler : ICommandHandler<SuspendTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuspendTenantCommandHandler"/> class.
    /// </summary>
    /// <param name="tenantRepository">Tenant repository.</param>
    /// <param name="unitOfWork">Unit of work for committing transaction.</param>
    public SuspendTenantCommandHandler(ITenantRepository tenantRepository, IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(SuspendTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await _tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant not found for the specified identifier."));
        }

        var suspendResult = tenant.Suspend(command.Reason);
        if (suspendResult.IsFailure)
        {
            return suspendResult;
        }

        _tenantRepository.Update(tenant);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
