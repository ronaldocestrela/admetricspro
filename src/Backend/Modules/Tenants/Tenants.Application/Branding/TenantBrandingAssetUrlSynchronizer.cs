using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Branding;

/// <summary>
/// Sincroniza URLs do formulário com objetos no bucket antes de <see cref="TenantBranding.UpdateDetails"/>.
/// </summary>
public static class TenantBrandingAssetUrlSynchronizer
{
    /// <summary>
    /// Aplica regras de convivência URL externa × objeto armazenado.
    /// </summary>
    /// <param name="branding">Entidade mutável.</param>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="lightLogoUrl">URL proposta para logo claro.</param>
    /// <param name="darkLogoUrl">URL proposta para logo escuro.</param>
    /// <param name="faviconUrl">URL proposta para favicon.</param>
    /// <param name="urlResolver">Resolvedor de URL da API.</param>
    /// <param name="store">Store de ativos.</param>
    /// <param name="requestOrigin">Origem HTTPS da requisição.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso ou falha de storage.</returns>
    public static async Task<Result> SyncAsync(
        TenantBranding branding,
        Guid tenantId,
        string? lightLogoUrl,
        string? darkLogoUrl,
        string? faviconUrl,
        ITenantBrandPublicUrlResolver urlResolver,
        ITenantBrandAssetStore store,
        string? requestOrigin,
        CancellationToken cancellationToken)
    {
        var light = await SyncSlotAsync(branding, tenantId, TenantBrandAssetSlot.Light, lightLogoUrl, urlResolver, store, requestOrigin, cancellationToken);
        if (light.IsFailure)
        {
            return light;
        }

        var dark = await SyncSlotAsync(branding, tenantId, TenantBrandAssetSlot.Dark, darkLogoUrl, urlResolver, store, requestOrigin, cancellationToken);
        if (dark.IsFailure)
        {
            return dark;
        }

        return await SyncSlotAsync(branding, tenantId, TenantBrandAssetSlot.Favicon, faviconUrl, urlResolver, store, requestOrigin, cancellationToken);
    }

    /// <summary>
    /// Obtém as URLs efetivas após sincronização para chamada a <see cref="TenantBranding.UpdateDetails"/>.
    /// </summary>
    /// <param name="branding">Entidade atualizada.</param>
    /// <returns>URLs por slot.</returns>
    public static (string? Light, string? Dark, string? Favicon) GetEffectiveUrls(TenantBranding branding) =>
        (branding.LightLogoUrl, branding.DarkLogoUrl, branding.FaviconUrl);

    private static async Task<Result> SyncSlotAsync(
        TenantBranding branding,
        Guid tenantId,
        TenantBrandAssetSlot slot,
        string? incomingUrl,
        ITenantBrandPublicUrlResolver urlResolver,
        ITenantBrandAssetStore store,
        string? requestOrigin,
        CancellationToken cancellationToken)
    {
        var kind = TenantBrandAssetKindMapping.ToDomainKind(slot);
        var incoming = string.IsNullOrWhiteSpace(incomingUrl) ? null : incomingUrl.Trim();
        var objectKey = branding.GetStoredObjectKey(kind);

        if (string.IsNullOrWhiteSpace(incoming))
        {
            if (!string.IsNullOrWhiteSpace(objectKey))
            {
                var delete = await store.DeleteAsync(objectKey, cancellationToken);
                if (delete.IsFailure)
                {
                    return delete;
                }
            }

            branding.ClearStoredAsset(kind);
            return Result.Success();
        }

        if (urlResolver.IsResolvedApiUrl(tenantId, slot, incoming, requestOrigin))
        {
            if (!string.IsNullOrWhiteSpace(objectKey))
            {
                var resolved = urlResolver.Resolve(tenantId, slot, requestOrigin);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    branding.SetStoredAssetPublicUrl(kind, resolved);
                }
            }
            else
            {
                branding.ApplyExternalUrl(kind, incoming);
            }

            return Result.Success();
        }

        if (!string.IsNullOrWhiteSpace(objectKey))
        {
            var delete = await store.DeleteAsync(objectKey, cancellationToken);
            if (delete.IsFailure)
            {
                return delete;
            }
        }

        branding.ApplyExternalUrl(kind, incoming);
        return Result.Success();
    }
}