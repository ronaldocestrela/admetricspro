using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Branding.DTOs;

namespace Tenants.Application.Branding;

/// <summary>
/// Mapeia <see cref="TenantBranding"/> para DTO com URLs públicas resolvidas quando há objeto.
/// </summary>
public static class TenantBrandingDetailsMapper
{
    /// <summary>
    /// Cria o DTO de detalhes com URLs de leitura pública quando aplicável.
    /// </summary>
    /// <param name="branding">Entidade de branding.</param>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="urlResolver">Resolvedor de URLs da API.</param>
    /// <param name="requestOrigin">Origem HTTPS da requisição (fallback).</param>
    /// <returns>DTO pronto para resposta HTTP.</returns>
    public static TenantBrandingDetailsDto ToDetailsDto(
        TenantBranding branding,
        Guid tenantId,
        ITenantBrandPublicUrlResolver urlResolver,
        string? requestOrigin) =>
        new(
            branding.PrimaryColor,
            branding.SecondaryColor,
            ResolveSlotUrl(branding, tenantId, TenantBrandAssetSlot.Light, urlResolver, requestOrigin),
            ResolveSlotUrl(branding, tenantId, TenantBrandAssetSlot.Dark, urlResolver, requestOrigin),
            ResolveSlotUrl(branding, tenantId, TenantBrandAssetSlot.Favicon, urlResolver, requestOrigin),
            branding.UpdatedAtUtc ?? branding.CreatedAtUtc);

    private static string? ResolveSlotUrl(
        TenantBranding branding,
        Guid tenantId,
        TenantBrandAssetSlot slot,
        ITenantBrandPublicUrlResolver urlResolver,
        string? requestOrigin)
    {
        var kind = TenantBrandAssetKindMapping.ToDomainKind(slot);
        if (!string.IsNullOrWhiteSpace(branding.GetStoredObjectKey(kind)))
        {
            return urlResolver.Resolve(tenantId, slot, requestOrigin) ?? branding.GetUrlForKind(kind);
        }

        return branding.GetUrlForKind(kind);
    }
}