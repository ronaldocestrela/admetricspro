namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Identificador do slot de ativo visual persistido no object storage.
/// </summary>
public enum TenantBrandStoredAssetKind
{
    /// <summary>Logomarca para tema claro.</summary>
    Light = 1,

    /// <summary>Logomarca para tema escuro.</summary>
    Dark = 2,

    /// <summary>Favicon do navegador.</summary>
    Favicon = 3,
}