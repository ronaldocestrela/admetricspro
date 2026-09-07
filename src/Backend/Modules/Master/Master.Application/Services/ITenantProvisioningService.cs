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
/// <param name="Segment">Optional market segment or business domain.</param>
/// <param name="MonthlyAdSpendRange">Optional estimated monthly ad spend range.</param>
/// <param name="BillingCycle">Optional subscription billing cycle frequency (Monthly or Annual).</param>
/// <param name="CustomDomain">Optional custom CNAME domain for white-label routing.</param>
/// <param name="PrimaryColor">Optional primary theme hex color code.</param>
/// <param name="SecondaryColor">Optional secondary theme hex color code.</param>
/// <param name="AdminFullName">Optional full name of the initial tenant administrator (Owner).</param>
/// <param name="AdminEmail">Optional corporate email address of the initial tenant administrator (Owner).</param>
/// <param name="AdminPhone">Optional contact phone number of the initial tenant administrator.</param>
/// <param name="AdminPassword">Optional plain text password for initial administrator seeding with secure hashing.</param>
public sealed record ProvisionTenantCommand(
    string CompanyName,
    string Cnpj,
    string Subdomain,
    SubscriptionTier Tier = SubscriptionTier.Trial,
    string? Segment = null,
    string? MonthlyAdSpendRange = null,
    string? BillingCycle = null,
    string? CustomDomain = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null,
    string? AdminFullName = null,
    string? AdminEmail = null,
    string? AdminPhone = null,
    string? AdminPassword = null);

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