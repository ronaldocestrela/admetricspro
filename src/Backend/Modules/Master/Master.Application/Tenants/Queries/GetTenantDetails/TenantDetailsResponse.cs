namespace Master.Application.Tenants.Queries.GetTenantDetails;

/// <summary>
/// Safe projection response containing tenant directory details without exposing sensitive connection credentials.
/// </summary>
/// <param name="Id">Unique identifier of the tenant.</param>
/// <param name="CompanyName">Legal or commercial name of the tenant enterprise.</param>
/// <param name="Cnpj">14-digit numeric CNPJ identifier.</param>
/// <param name="Subdomain">Designated routing subdomain for tenant isolation.</param>
/// <param name="Status">Tenant lifecycle status description.</param>
/// <param name="Tier">Assigned subscription tier level.</param>
/// <param name="SubscriptionExpiresAtUtc">UTC timestamp indicating subscription or trial expiration, if applicable.</param>
/// <param name="CreatedAtUtc">UTC timestamp when the tenant was provisioned.</param>
public sealed record TenantDetailsResponse(
    Guid Id,
    string CompanyName,
    string Cnpj,
    string Subdomain,
    string Status,
    string Tier,
    DateTime? SubscriptionExpiresAtUtc,
    DateTime CreatedAtUtc);
