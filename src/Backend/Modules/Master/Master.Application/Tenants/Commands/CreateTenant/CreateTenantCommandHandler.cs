using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Services;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.CreateTenant;

/// <summary>
/// Handles <see cref="CreateTenantCommand"/> by orchestrating tenant database provisioning and metadata registration.
/// </summary>
public sealed class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, TenantId>
{
    private readonly ITenantProvisioningService _provisioningService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTenantCommandHandler"/> class.
    /// </summary>
    /// <param name="provisioningService">Tenant provisioning service.</param>
    public CreateTenantCommandHandler(ITenantProvisioningService provisioningService)
    {
        _provisioningService = provisioningService;
    }

    /// <inheritdoc />
    public Task<Result<TenantId>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var provisioningCommand = new ProvisionTenantCommand(
            command.CompanyName,
            command.Cnpj,
            command.Subdomain,
            command.Tier);

        return _provisioningService.ProvisionTenantDatabaseAsync(provisioningCommand, cancellationToken);
    }
}
