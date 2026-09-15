namespace BuildingBlocks.Application.MultiTenancy;

/// <summary>
/// Defines the origin or channel from which tenant identity was extracted in the request pipeline.
/// </summary>
public enum TenantResolutionSource
{
    /// <summary>
    /// No tenant identity was identified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Tenant identity was extracted from an HTTP request header (e.g., X-Tenant-Id).
    /// </summary>
    Header = 1,

    /// <summary>
    /// Tenant identity was extracted from an authenticated user JWT claim.
    /// </summary>
    JwtClaim = 2,

    /// <summary>
    /// Tenant identity was extracted from the host subdomain.
    /// </summary>
    Subdomain = 3,

    /// <summary>
    /// Tenant identity was extracted from a custom domain mapped via CNAME (e.g. relatorios.agencia.com.br).
    /// </summary>
    CustomDomain = 4
}
