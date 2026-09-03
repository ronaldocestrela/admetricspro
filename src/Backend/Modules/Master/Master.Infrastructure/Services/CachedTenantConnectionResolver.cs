using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Security;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Caching.Memory;

namespace Master.Infrastructure.Services;

/// <summary>
/// Resolves tenant database connection strings from the Master catalog and caches decrypted values securely in memory.
/// </summary>
public sealed class CachedTenantConnectionResolver : ITenantConnectionResolver
{
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromHours(4);

    private readonly ITenantRepository _tenantRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedTenantConnectionResolver"/> class.
    /// </summary>
    /// <param name="tenantRepository">Tenant repository for catalog lookups.</param>
    /// <param name="encryptionService">Encryption service for decrypting connection strings.</param>
    /// <param name="tenantContextAccessor">Context accessor for reading active request tenant identity.</param>
    /// <param name="memoryCache">In-memory cache provider.</param>
    public CachedTenantConnectionResolver(
        ITenantRepository tenantRepository,
        IEncryptionService encryptionService,
        ITenantContextAccessor tenantContextAccessor,
        IMemoryCache memoryCache)
    {
        _tenantRepository = tenantRepository;
        _encryptionService = encryptionService;
        _tenantContextAccessor = tenantContextAccessor;
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    public async Task<Result<string>> ResolveConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<string>.Failure(Error.Validation("Tenant.InvalidId", "Tenant identifier cannot be empty."));
        }

        var cacheKey = GetIdCacheKey(tenantId);
        if (_memoryCache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return Result<string>.Success(cached);
        }

        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(tenantId), cancellationToken);
        if (tenant is null)
        {
            return Result<string>.Failure(Error.NotFound("Tenant.NotFound", $"Tenant with identifier '{tenantId}' was not found in catalog."));
        }

        return ProcessAndCacheTenant(tenant);
    }

    /// <inheritdoc />
    public async Task<Result<string>> ResolveConnectionStringBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return Result<string>.Failure(Error.Validation("Tenant.InvalidSubdomain", "Subdomain cannot be empty."));
        }

        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        var cacheKey = GetSubdomainCacheKey(normalizedSubdomain);
        if (_memoryCache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return Result<string>.Success(cached);
        }

        var tenant = await _tenantRepository.GetBySubdomainAsync(normalizedSubdomain, cancellationToken);
        if (tenant is null)
        {
            return Result<string>.Failure(Error.NotFound("Tenant.NotFound", $"Tenant with subdomain '{normalizedSubdomain}' was not found in catalog."));
        }

        return ProcessAndCacheTenant(tenant);
    }

    /// <inheritdoc />
    public async Task<Result<string>> ResolveCurrentTenantConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        var context = _tenantContextAccessor.TenantContext;
        if (context is null || !context.IsResolved)
        {
            return Result<string>.Failure(Error.NotFound("Tenant.ContextNotResolved", "No tenant context has been resolved for the current operation."));
        }

        if (context.TenantId.HasValue && context.TenantId.Value != Guid.Empty)
        {
            return await ResolveConnectionStringAsync(context.TenantId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(context.Subdomain))
        {
            return await ResolveConnectionStringBySubdomainAsync(context.Subdomain, cancellationToken);
        }

        return Result<string>.Failure(Error.NotFound("Tenant.IdentifierMissing", "The resolved context does not contain a usable tenant identifier."));
    }

    /// <inheritdoc />
    public void InvalidateCache(Guid tenantId)
    {
        _memoryCache.Remove(GetIdCacheKey(tenantId));
    }

    /// <inheritdoc />
    public void InvalidateCacheBySubdomain(string subdomain)
    {
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            _memoryCache.Remove(GetSubdomainCacheKey(subdomain.Trim().ToLowerInvariant()));
        }
    }

    private Result<string> ProcessAndCacheTenant(Tenant tenant)
    {
        if (tenant.Status == TenantStatus.Suspended || tenant.Status == TenantStatus.Cancelled)
        {
            return Result<string>.Failure(Error.Validation("Tenant.Inactive", $"Tenant '{tenant.CompanyName}' is currently {tenant.Status}."));
        }

        if (string.IsNullOrWhiteSpace(tenant.EncryptedConnectionString))
        {
            return Result<string>.Failure(Error.Validation("Tenant.ConnectionStringMissing", "Encrypted connection string is missing for tenant."));
        }

        string plainConnectionString;
        try
        {
            plainConnectionString = _encryptionService.Decrypt(tenant.EncryptedConnectionString);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(Error.Failure("Tenant.DecryptionFailed", $"Failed to decrypt connection string: {ex.Message}"));
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultSlidingExpiration,
            AbsoluteExpirationRelativeToNow = DefaultAbsoluteExpiration
        };

        _memoryCache.Set(GetIdCacheKey(tenant.Id.Value), plainConnectionString, cacheEntryOptions);
        _memoryCache.Set(GetSubdomainCacheKey(tenant.Subdomain), plainConnectionString, cacheEntryOptions);

        return Result<string>.Success(plainConnectionString);
    }

    private static string GetIdCacheKey(Guid tenantId) => $"Tenant_Conn_Id_{tenantId:D}";
    private static string GetSubdomainCacheKey(string subdomain) => $"Tenant_Conn_Subdomain_{subdomain}";
}
