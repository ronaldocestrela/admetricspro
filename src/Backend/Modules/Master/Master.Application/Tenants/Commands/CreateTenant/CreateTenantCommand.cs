using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.CreateTenant;

/// <summary>
/// Command to create and provision a dedicated tenant database in the SaaS platform.
/// </summary>
/// <param name="CompanyName">Legal or commercial name of the enterprise.</param>
/// <param name="Cnpj">14-digit numeric CNPJ identifier.</param>
/// <param name="Subdomain">Designated routing subdomain for tenant isolation.</param>
/// <param name="Tier">Initial subscription tier level.</param>
public sealed record CreateTenantCommand(
    string CompanyName,
    string Cnpj,
    string Subdomain,
    SubscriptionTier Tier = SubscriptionTier.Trial) : ICommand<TenantId>;
