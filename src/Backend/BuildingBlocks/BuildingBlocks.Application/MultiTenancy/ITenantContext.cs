namespace BuildingBlocks.Application.MultiTenancy;

/// <summary>
/// Provides read-only access to the resolved tenant contextual information for the current execution scope.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the unique identifier of the tenant when resolved via GUID.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the tenant subdomain or unique slug when resolved via host or slug header.
    /// </summary>
    string? Subdomain { get; }

    /// <summary>
    /// Gets the raw identifier extracted from the request before resolution or parsing.
    /// </summary>
    string? RawIdentifier { get; }

    /// <summary>
    /// Gets the source channel from which tenant identity was identified.
    /// </summary>
    TenantResolutionSource Source { get; }

    /// <summary>
    /// Gets a value indicating whether a valid tenant identity was resolved in the current context.
    /// </summary>
    bool IsResolved { get; }
}
