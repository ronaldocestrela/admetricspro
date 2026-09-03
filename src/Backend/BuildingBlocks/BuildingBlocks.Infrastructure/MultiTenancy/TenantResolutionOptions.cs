using BuildingBlocks.Application.MultiTenancy;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Configuration options for dynamic multi-tenant identification and resolution.
/// </summary>
public sealed class TenantResolutionOptions
{
    /// <summary>
    /// Gets or sets the HTTP request header name used to pass tenant identity.
    /// Default is "X-Tenant-Id".
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// Gets or sets the JWT claim type used to extract tenant identity.
    /// Default is "tenant_id".
    /// </summary>
    public string JwtClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets the collection of base domains used to isolate tenant subdomains from incoming hosts.
    /// Default includes "admetricspro.com", "app.com", and "localhost".
    /// </summary>
    public IReadOnlyList<string> BaseDomains { get; set; } = ["admetricspro.com", "app.com", "localhost"];

    /// <summary>
    /// Gets or sets the list of reserved subdomains that should never resolve to a tenant.
    /// Default includes "www", "api", "admin", "app", and "mail".
    /// </summary>
    public IReadOnlyList<string> ReservedSubdomains { get; set; } = ["www", "api", "admin", "app", "mail"];

    /// <summary>
    /// Gets or sets the order in which identification channels are evaluated.
    /// </summary>
    public IReadOnlyList<TenantResolutionSource> ResolutionOrder { get; set; } =
    [
        TenantResolutionSource.Header,
        TenantResolutionSource.JwtClaim,
        TenantResolutionSource.Subdomain
    ];
}
