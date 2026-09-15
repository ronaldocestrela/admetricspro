using System.Net;
using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.MultiTenancy.Strategies;

/// <summary>
/// Estratégia de identificação de inquilino para domínios próprios mapeados via registro DNS CNAME (ex: relatorios.agenciaalfa.com.br).
/// </summary>
public sealed class CustomDomainTenantIdentificationStrategy : ITenantIdentificationStrategy
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromMinutes(5);

    private readonly TenantResolutionOptions _options;
    private readonly ITenantCustomDomainResolver _customDomainResolver;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CustomDomainTenantIdentificationStrategy"/>.
    /// </summary>
    /// <param name="options">Opções de resolução de tenant.</param>
    /// <param name="customDomainResolver">Resolvedor de mapeamento de domínio CNAME.</param>
    /// <param name="memoryCache">Provedor de cache em memória.</param>
    public CustomDomainTenantIdentificationStrategy(
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
    public TenantResolutionSource Source => TenantResolutionSource.CustomDomain;

    /// <inheritdoc />
    public async ValueTask<TenantIdentificationResult?> IdentifyTenantAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        // Ignora requisições diretas a endereços IP
        if (IPAddress.TryParse(host, out _))
        {
            return null;
        }

        // Ignora requisições que pertencem aos domínios base (essas devem ser tratadas por SubdomainTenantIdentificationStrategy)
        foreach (var baseDomain in _options.BaseDomains)
        {
            var normalizedBaseDomain = baseDomain.Trim().TrimStart('.');
            if (host.Equals(normalizedBaseDomain, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + normalizedBaseDomain, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        var normalizedHost = host.Trim().ToLowerInvariant();
        var cacheKey = $"Tenant_CustomDomain_{normalizedHost}";

        if (_memoryCache.TryGetValue(cacheKey, out TenantIdentificationResult? cachedResult))
        {
            return cachedResult;
        }

        var mapping = await _customDomainResolver.ResolveTenantByCustomDomainAsync(normalizedHost, cancellationToken);
        if (mapping is not null)
        {
            var result = TenantIdentificationResult.FromCustomDomain(
                mapping.TenantId,
                mapping.Subdomain,
                normalizedHost);

            _memoryCache.Set(cacheKey, result, CacheDuration);
            return result;
        }

        // Armazena resultado nulo por curto período para evitar sobrecarga no banco com hosts inexistentes
        _memoryCache.Set<TenantIdentificationResult?>(cacheKey, null, NegativeCacheDuration);
        return null;
    }
}
