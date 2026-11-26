using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Branding;

/// <summary>
/// Converte entre enum de aplicação e enum de domínio.
/// </summary>
public static class TenantBrandAssetKindMapping
{
    /// <summary>
    /// Converte o slot de aplicação para o kind de domínio.
    /// </summary>
    /// <param name="slot">Slot de aplicação.</param>
    /// <returns>Kind de domínio equivalente.</returns>
    public static TenantBrandStoredAssetKind ToDomainKind(TenantBrandAssetSlot slot) =>
        slot switch
        {
            TenantBrandAssetSlot.Light => TenantBrandStoredAssetKind.Light,
            TenantBrandAssetSlot.Dark => TenantBrandStoredAssetKind.Dark,
            TenantBrandAssetSlot.Favicon => TenantBrandStoredAssetKind.Favicon,
            _ => throw new ArgumentOutOfRangeException(nameof(slot)),
        };
}