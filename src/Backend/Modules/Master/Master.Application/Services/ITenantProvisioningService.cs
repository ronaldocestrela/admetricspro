using BuildingBlocks.Domain.Primitives;
using Master.Domain.Tenants;

namespace Master.Application.Services;

/// <summary>
/// Provisions dedicated tenant databases in SQL Server and applies schema migrations.
/// </summary>
public interface ITenantProvisioningService
{
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