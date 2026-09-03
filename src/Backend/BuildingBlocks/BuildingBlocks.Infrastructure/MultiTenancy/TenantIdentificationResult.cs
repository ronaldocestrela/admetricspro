using BuildingBlocks.Application.MultiTenancy;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Encapsulates the output of a tenant identification strategy execution.
/// </summary>
public sealed class TenantIdentificationResult
{
    private TenantIdentificationResult(
        Guid? tenantId,
        string? subdomain,
        TenantResolutionSource source,
        string rawIdentifier)
    {
        TenantId = tenantId;
        Subdomain = subdomain;
        Source = source;
        RawIdentifier = rawIdentifier;
    }

    /// <summary>
    /// Gets the resolved tenant GUID, if available.
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>
    /// Gets the resolved tenant subdomain or slug, if available.
    /// </summary>
    public string? Subdomain { get; }

    /// <summary>
    /// Gets the channel that identified the tenant.
    /// </summary>
    public TenantResolutionSource Source { get; }

    /// <summary>
    /// Gets the raw string value extracted from the request.
    /// </summary>
    public string RawIdentifier { get; }

    /// <summary>
    /// Creates a result based on a valid tenant GUID.
    /// </summary>
    /// <param name="tenantId">Tenant GUID.</param>
    /// <param name="source">Resolution channel source.</param>
    /// <param name="rawIdentifier">Original raw value.</param>
    /// <returns>A new <see cref="TenantIdentificationResult"/> instance.</returns>
    public static TenantIdentificationResult FromTenantId(Guid tenantId, TenantResolutionSource source, string rawIdentifier) =>
        new(tenantId, null, source, rawIdentifier);

    /// <summary>
    /// Creates a result based on a tenant subdomain/slug.
    /// </summary>
    /// <param name="subdomain">Tenant subdomain.</param>
    /// <param name="source">Resolution channel source.</param>
    /// <param name="rawIdentifier">Original raw value.</param>
    /// <returns>A new <see cref="TenantIdentificationResult"/> instance.</returns>
    public static TenantIdentificationResult FromSubdomain(string subdomain, TenantResolutionSource source, string rawIdentifier) =>
        new(null, subdomain, source, rawIdentifier);
}
