using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.ReactivateTenant;

/// <summary>
/// Handles <see cref="ReactivateTenantCommand"/> to reactivate a suspended tenant.
/// </summary>
public sealed class ReactivateTenantCommandHandler : ICommandHandler<ReactivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactivateTenantCommandHandler"/> class.
    /// </summary>
    /// <param name="tenantRepository">Tenant repository.</param>
    /// <param name="unitOfWork">Unit of work for committing transaction.</param>
    public ReactivateTenantCommandHandler(ITenantRepository tenantRepository, IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await _tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant not found for the specified identifier."));
        }

        var reactivateResult = tenant.Reactivate();
        if (reactivateResult.IsFailure)
        {
            return reactivateResult;
        }

        _tenantRepository.Update(tenant);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
