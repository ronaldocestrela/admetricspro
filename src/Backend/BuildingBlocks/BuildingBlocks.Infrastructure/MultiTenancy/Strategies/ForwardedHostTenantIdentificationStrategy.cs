using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.MultiTenancy.Strategies;

/// <summary>
/// Resolve o tenant a partir do host original do browser repassado pela Web API (ex.: portal de aprovação Blazor).
/// </summary>
public sealed class ForwardedHostTenantIdentificationStrategy : ITenantIdentificationStrategy
{
    private readonly TenantResolutionOptions _options;
    private readonly ITenantCustomDomainResolver _customDomainResolver;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ForwardedHostTenantIdentificationStrategy"/>.
    /// </summary>
    /// <param name="options">Opções de resolução.</param>
    /// <param name="customDomainResolver">Resolvedor de CNAME.</param>
    /// <param name="memoryCache">Cache de mapeamentos.</param>
    public ForwardedHostTenantIdentificationStrategy(
        IOptions<TenantResolutionOptions> options,
        ITenantCustomDomainResolver customDomainResolver,
        IMemoryCache memoryCache)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(customDomainResolver);
        ArgumentNullException.ThrowIfNull(memoryCache);
        _options = options.Value;
        _customDomainResolver = customDomainResolver;
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public TenantResolutionSource Source => TenantResolutionSource.ForwardedHost;

    /// <inheritdoc />
    public async ValueTask<TenantIdentificationResult?> IdentifyTenantAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!httpContext.Request.Headers.TryGetValue(_options.ForwardedHostHeaderName, out var headerValues))
        {
            return null;
        }

        var host = headerValues.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var subdomainResult = TryResolveSubdomain(host);
        if (subdomainResult is not null)
        {
            return subdomainResult;
        }

        return await TryResolveCustomDomainAsync(host, cancellationToken);
    }

    private TenantIdentificationResult? TryResolveSubdomain(string host)
    {
        foreach (var baseDomain in _options.BaseDomains)
        {
            var normalizedBaseDomain = baseDomain.Trim().TrimStart('.');
            var suffix = "." + normalizedBaseDomain;

            if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var prefix = host[..^suffix.Length];
            if (string.IsNullOrWhiteSpace(prefix))
            {
                continue;
            }

            var subdomain = prefix.Split('.')[0].Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(subdomain) ||
                _options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            return TenantIdentificationResult.FromSubdomain(subdomain, Source, host);
        }

        return null;
    }

    private async Task<TenantIdentificationResult?> TryResolveCustomDomainAsync(
        string host,
        CancellationToken cancellationToken)
    {
        var normalizedHost = host.Trim().ToLowerInvariant();
        var cacheKey = $"Tenant_ForwardedHost_{normalizedHost}";

        if (_memoryCache.TryGetValue(cacheKey, out TenantIdentificationResult? cached))
        {
            return cached;
        }

        var mapping = await _customDomainResolver.ResolveTenantByCustomDomainAsync(normalizedHost, cancellationToken);
        if (mapping is null)
        {
            _memoryCache.Set<TenantIdentificationResult?>(cacheKey, null, TimeSpan.FromMinutes(5));
            return null;
        }

        var result = TenantIdentificationResult.FromCustomDomain(
            mapping.TenantId,
            mapping.Subdomain,
            normalizedHost);
        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
        return result;
    }
}
