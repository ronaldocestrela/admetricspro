namespace Tenants.Application.Branding;

/// <summary>
/// Slot de ativo visual white-label armazenado no bucket de branding.
/// </summary>
public enum TenantBrandAssetSlot
{
    /// <summary>Logomarca para tema claro.</summary>
    Light = 1,

    /// <summary>Logomarca para tema escuro.</summary>
    Dark = 2,

    /// <summary>Favicon do navegador.</summary>
    Favicon = 3,
}