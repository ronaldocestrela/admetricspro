using BuildingBlocks.Application.MultiTenancy;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Default immutable implementation of <see cref="ITenantContext"/>.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private static readonly TenantContext EmptyInstance = new(null, null, null, TenantResolutionSource.None);

    private TenantContext(Guid? tenantId, string? subdomain, string? rawIdentifier, TenantResolutionSource source)
    {
        TenantId = tenantId;
        Subdomain = subdomain;
        RawIdentifier = rawIdentifier;
        Source = source;
    }

    /// <inheritdoc />
    public Guid? TenantId { get; }

    /// <inheritdoc />
    public string? Subdomain { get; }

    /// <inheritdoc />
    public string? RawIdentifier { get; }

    /// <inheritdoc />
    public TenantResolutionSource Source { get; }

    /// <inheritdoc />
    public bool IsResolved => TenantId.HasValue || !string.IsNullOrWhiteSpace(Subdomain);

    /// <summary>
    /// Gets a singleton instance representing an unassigned or unresolved tenant context.
    /// </summary>
    public static TenantContext Empty => EmptyInstance;

    /// <summary>
    /// Creates a resolved tenant context.
    /// </summary>
    /// <param name="tenantId">Optional tenant GUID.</param>
    /// <param name="subdomain">Optional tenant subdomain.</param>
    /// <param name="source">Source channel where identification occurred.</param>
    /// <param name="rawIdentifier">Optional raw identifier string.</param>
    /// <returns>A populated tenant context instance.</returns>
    public static TenantContext Create(
        Guid? tenantId,
        string? subdomain,
        TenantResolutionSource source,
        string? rawIdentifier = null)
    {
        return new TenantContext(tenantId, subdomain, rawIdentifier, source);
    }
}
