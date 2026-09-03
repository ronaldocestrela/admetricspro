using BuildingBlocks.Domain.Primitives;
using Master.Domain.Tenants;

namespace Master.Application.Services;

/// <summary>
/// Structured command representing input parameters for provisioning a dedicated tenant database.
/// </summary>
/// <param name="CompanyName">Legal or commercial name of the tenant enterprise.</param>
/// <param name="Cnpj">CNPJ digits-only identifier (exactly 14 numeric characters).</param>
/// <param name="Subdomain">Designated routing subdomain for tenant isolation.</param>
/// <param name="Tier">Initial subscription tier. Defaults to <see cref="SubscriptionTier.Trial"/>.</param>
public sealed record ProvisionTenantCommand(
    string CompanyName,
    string Cnpj,
    string Subdomain,
    SubscriptionTier Tier = SubscriptionTier.Trial);

/// <summary>
/// Provisions dedicated tenant databases in SQL Server and applies schema migrations.
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>
    /// Executes dynamic provisioning using a structured command, creates the dedicated database, and applies migrations.
    /// </summary>
    /// <param name="command">Structured provisioning command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provisioning result containing the new <see cref="TenantId"/> on success, or a typed domain error.</returns>
    Task<Result<TenantId>> ProvisionTenantDatabaseAsync(
        ProvisionTenantCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a tenant database, applies tenant schema, and returns the new tenant identifier.
    /// </summary>
    /// <param name="companyName">Tenant company name.</param>
    /// <param name="cnpj">Tenant CNPJ (14 digits).</param>
    /// <param name="subdomain">Tenant subdomain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provisioning result containing the new tenant identifier on success.</returns>
    Task<Result<TenantId>> ProvisionTenantDatabaseAsync(
        string companyName,
        string cnpj,
        string subdomain,
        CancellationToken cancellationToken);
}