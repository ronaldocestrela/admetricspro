namespace Tenants.Application.Branding;

/// <summary>
/// Limites de produto para upload de ativos de marca.
/// </summary>
public static class TenantBrandingAssetLimits
{
    /// <summary>Tamanho máximo em bytes (2 MB).</summary>
    public const int MaxUploadBytes = 2_097_152;

    /// <summary>Limite de leitura para inspeção de SVG (caracteres).</summary>
    public const int MaxSvgInspectChars = 64_000;
}