using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Services;

namespace Master.Application.Tenants.Commands.RegisterTenantOnboarding;

/// <summary>
/// Manipulador do comando <see cref="RegisterTenantOnboardingCommand"/>.
/// Coordena o provisionamento automático do banco dedicado do novo inquilino e emite as credenciais de acesso.
/// </summary>
public sealed class RegisterTenantOnboardingCommandHandler : ICommandHandler<RegisterTenantOnboardingCommand, TenantOnboardingResult>
{
    private readonly ITenantProvisioningService _provisioningService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RegisterTenantOnboardingCommandHandler"/>.
    /// </summary>
    /// <param name="provisioningService">Serviço de provisionamento de banco isolado de tenant.</param>
    public RegisterTenantOnboardingCommandHandler(ITenantProvisioningService provisioningService)
    {
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));
    }

    /// <inheritdoc />
    public async Task<Result<TenantOnboardingResult>> Handle(RegisterTenantOnboardingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var provisioningCommand = new ProvisionTenantCommand(
            command.CompanyName.Trim(),
            command.Cnpj.Trim(),
            command.Subdomain.Trim().ToLowerInvariant(),
            command.Tier,
            command.Segment?.Trim(),
            command.MonthlyAdSpendRange?.Trim(),
            command.BillingCycle?.Trim(),
            command.CustomDomain?.Trim(),
            command.PrimaryColor?.Trim(),
            command.SecondaryColor?.Trim());

        var provisioningResult = await _provisioningService.ProvisionTenantDatabaseAsync(
            provisioningCommand,
            cancellationToken);

        if (provisioningResult.IsFailure)
        {
            return Result<TenantOnboardingResult>.Failure(provisioningResult.Error);
        }

        var tenantId = provisioningResult.Value;
        var sanitizedSubdomain = command.Subdomain.Trim().ToLowerInvariant();
        var accessUrl = $"https://{sanitizedSubdomain}.admetricspro.com.br/dashboard";

        var onboardingResult = new TenantOnboardingResult(
            tenantId.Value,
            command.CompanyName.Trim(),
            sanitizedSubdomain,
            accessUrl,
            command.AdminEmail.Trim().ToLowerInvariant(),
            command.Tier);

        return Result<TenantOnboardingResult>.Success(onboardingResult);
    }
}
