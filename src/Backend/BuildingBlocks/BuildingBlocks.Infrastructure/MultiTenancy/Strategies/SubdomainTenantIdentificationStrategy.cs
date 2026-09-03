using System.Net;
using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.MultiTenancy.Strategies;

/// <summary>
/// Identifies tenant identity from the HTTP request host subdomain (e.g., agencia-alfa.admetricspro.com or cliente.localhost).
/// </summary>
public sealed class SubdomainTenantIdentificationStrategy : ITenantIdentificationStrategy
{
    private readonly TenantResolutionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubdomainTenantIdentificationStrategy"/> class.
    /// </summary>
    /// <param name="options">Tenant resolution configuration options.</param>
    public SubdomainTenantIdentificationStrategy(IOptions<TenantResolutionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public TenantResolutionSource Source => TenantResolutionSource.Subdomain;

    /// <inheritdoc />
    public ValueTask<TenantIdentificationResult?> IdentifyTenantAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(null);
        }

        // Ignore direct IP access
        if (IPAddress.TryParse(host, out _))
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(null);
        }

        foreach (var baseDomain in _options.BaseDomains)
        {
            var normalizedBaseDomain = baseDomain.Trim().TrimStart('.');
            var suffix = "." + normalizedBaseDomain;

            if (host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var prefix = host[..^suffix.Length];
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                // If nested, choose the leading tenant label (e.g. "tenant.staging" -> "tenant")
                var subdomain = prefix.Split('.')[0].Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(subdomain))
                {
                    continue;
                }

                if (_options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
                {
                    return ValueTask.FromResult<TenantIdentificationResult?>(null);
                }

                return ValueTask.FromResult<TenantIdentificationResult?>(
                    TenantIdentificationResult.FromSubdomain(subdomain, Source, subdomain));
            }
        }

        return ValueTask.FromResult<TenantIdentificationResult?>(null);
    }
}
