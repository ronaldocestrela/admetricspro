using BuildingBlocks.Application.MultiTenancy;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Provedor nulo de resolução de domínios customizados utilizado como fallback padrão quando o catálogo MasterDb não está presente.
/// </summary>
public sealed class NullTenantCustomDomainResolver : ITenantCustomDomainResolver
{
    /// <inheritdoc />
    public Task<TenantCustomDomainMapping?> ResolveTenantByCustomDomainAsync(
        string customDomain,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TenantCustomDomainMapping?>(null);
    }
}
