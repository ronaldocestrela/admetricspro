using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Tenants.Application.Branding;

namespace Tenants.Infrastructure.Branding;

/// <summary>
/// Implementação de <see cref="ITenantBrandPublicUrlResolver"/> usando <see cref="ObjectStorageOptions.PublicBaseUrl"/>.
/// </summary>
public sealed class TenantBrandPublicUrlResolver : ITenantBrandPublicUrlResolver
{
    private readonly ObjectStorageOptions _options;

    /// <summary>
    /// Inicializa o resolvedor.
    /// </summary>
    /// <param name="options">Opções de object storage.</param>
    public TenantBrandPublicUrlResolver(IOptions<ObjectStorageOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string? Resolve(Guid tenantId, TenantBrandAssetSlot slot, string? requestOrigin)
    {
        var origin = SelectOrigin(requestOrigin);
        if (string.IsNullOrWhiteSpace(origin))
        {
            return null;
        }

        var segment = TenantBrandAssetSlotParser.ToRouteSegment(slot);
        return $"{origin.TrimEnd('/')}/api/v1/public/branding/{tenantId:N}/{segment}";
    }

    /// <inheritdoc />
    public bool IsResolvedApiUrl(Guid tenantId, TenantBrandAssetSlot slot, string? url, string? requestOrigin)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var expected = Resolve(tenantId, slot, requestOrigin);
        if (expected is null)
        {
            return false;
        }

        return string.Equals(url.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private string? SelectOrigin(string? requestOrigin)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return _options.PublicBaseUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(requestOrigin))
        {
            return null;
        }

        return requestOrigin.Trim().TrimEnd('/');
    }
}