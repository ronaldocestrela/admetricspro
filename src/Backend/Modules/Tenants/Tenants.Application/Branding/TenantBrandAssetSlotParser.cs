using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Branding;

/// <summary>
/// Converte identificadores de rota em <see cref="TenantBrandAssetSlot"/>.
/// </summary>
public static class TenantBrandAssetSlotParser
{
    /// <summary>
    /// Falha de validação quando o segmento da rota é inválido.
    /// </summary>
    public static readonly Error InvalidSlot = Error.Validation(
        "TenantBranding.InvalidAssetSlot",
        "O slot de ativo deve ser light, dark ou favicon.");

    /// <summary>
    /// Tenta interpretar o segmento de rota como slot válido.
    /// </summary>
    /// <param name="routeSegment">Valor da rota (ex.: <c>light</c>).</param>
    /// <param name="slot">Slot parseado.</param>
    /// <returns><see langword="true"/> quando reconhecido.</returns>
    public static bool TryParse(string? routeSegment, out TenantBrandAssetSlot slot)
    {
        slot = default;
        if (string.IsNullOrWhiteSpace(routeSegment))
        {
            return false;
        }

        return routeSegment.Trim().ToLowerInvariant() switch
        {
            "light" => Set(TenantBrandAssetSlot.Light, out slot),
            "dark" => Set(TenantBrandAssetSlot.Dark, out slot),
            "favicon" => Set(TenantBrandAssetSlot.Favicon, out slot),
            _ => false,
        };
    }

    /// <summary>
    /// Converte o slot para o segmento usado na URL pública.
    /// </summary>
    /// <param name="slot">Slot do ativo.</param>
    /// <returns>Segmento em minúsculas.</returns>
    public static string ToRouteSegment(TenantBrandAssetSlot slot) =>
        slot switch
        {
            TenantBrandAssetSlot.Light => "light",
            TenantBrandAssetSlot.Dark => "dark",
            TenantBrandAssetSlot.Favicon => "favicon",
            _ => throw new ArgumentOutOfRangeException(nameof(slot)),
        };

    private static bool Set(TenantBrandAssetSlot value, out TenantBrandAssetSlot slot)
    {
        slot = value;
        return true;
    }
}