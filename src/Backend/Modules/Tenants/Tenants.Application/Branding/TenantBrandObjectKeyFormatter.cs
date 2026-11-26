namespace Tenants.Application.Branding;

/// <summary>
/// Formata chaves canônicas de ativos de branding no bucket <c>admetricspro-branding</c>.
/// </summary>
public static class TenantBrandObjectKeyFormatter
{
    /// <summary>
    /// Monta a chave <c>{tenantId:N}/{slot}</c>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do objeto.</param>
    /// <param name="slot">Slot do ativo.</param>
    /// <returns>Chave relativa no bucket.</returns>
    public static string Build(Guid tenantId, TenantBrandAssetSlot slot) =>
        $"{tenantId:N}/{TenantBrandAssetSlotParser.ToRouteSegment(slot)}";
}