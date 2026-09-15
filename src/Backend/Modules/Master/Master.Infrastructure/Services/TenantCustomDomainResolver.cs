using BuildingBlocks.Application.MultiTenancy;
using Master.Application.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace Master.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="ITenantCustomDomainResolver"/> que consulta o catálogo central MasterDb com cache seguro em memória.
/// </summary>
public sealed class TenantCustomDomainResolver : ITenantCustomDomainResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly ITenantRepository _tenantRepository;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantCustomDomainResolver"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório do catálogo de inquilinos.</param>
    /// <param name="memoryCache">Provedor de cache em memória.</param>
    public TenantCustomDomainResolver(
        ITenantRepository tenantRepository,
        IMemoryCache memoryCache)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    /// <inheritdoc />
    public async Task<TenantCustomDomainMapping?> ResolveTenantByCustomDomainAsync(
        string customDomain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customDomain))
        {
            return null;
        }

        var normalized = customDomain.Trim().ToLowerInvariant();
        var cacheKey = $"Tenant_CustomDomain_Mapping_{normalized}";

        if (_memoryCache.TryGetValue(cacheKey, out TenantCustomDomainMapping? cached))
        {
            return cached;
        }

        var tenant = await _tenantRepository.GetByCustomDomainAsync(normalized, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var mapping = new TenantCustomDomainMapping(
            tenant.Id.Value,
            tenant.Subdomain,
            normalized);

        _memoryCache.Set(cacheKey, mapping, CacheDuration);
        return mapping;
    }
}
