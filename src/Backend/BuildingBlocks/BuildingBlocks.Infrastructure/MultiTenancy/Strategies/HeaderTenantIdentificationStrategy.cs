using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.MultiTenancy.Strategies;

/// <summary>
/// Identifies tenant identity from a designated HTTP request header (default: X-Tenant-Id).
/// Supports GUID values or alphanumeric subdomain slugs.
/// </summary>
public sealed class HeaderTenantIdentificationStrategy : ITenantIdentificationStrategy
{
    private readonly TenantResolutionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderTenantIdentificationStrategy"/> class.
    /// </summary>
    /// <param name="options">Tenant resolution configuration options.</param>
    public HeaderTenantIdentificationStrategy(IOptions<TenantResolutionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public TenantResolutionSource Source => TenantResolutionSource.Header;

    /// <inheritdoc />
    public ValueTask<TenantIdentificationResult?> IdentifyTenantAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Request.Headers.TryGetValue(_options.HeaderName, out var headerValues))
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(null);
        }

        var headerValue = headerValues.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(null);
        }

        if (Guid.TryParse(headerValue, out var tenantId))
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(
                TenantIdentificationResult.FromTenantId(tenantId, Source, headerValue));
        }

        return ValueTask.FromResult<TenantIdentificationResult?>(
            TenantIdentificationResult.FromSubdomain(headerValue.ToLowerInvariant(), Source, headerValue));
    }
}
