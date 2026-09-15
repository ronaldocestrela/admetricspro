namespace WebApp.Models;

/// <summary>
/// Modelo de dados para edição e submissão da identidade visual White-Label do inquilino.
/// </summary>
public sealed class UpdateTenantBrandingModel
{
    /// <summary>
    /// Código hexadecimal da cor primária (ex: #2563EB).
    /// </summary>
    public string PrimaryColor { get; set; } = "#2563EB";

    /// <summary>
    /// Código hexadecimal da cor secundária (ex: #0F172A).
    /// </summary>
    public string SecondaryColor { get; set; } = "#0F172A";

    /// <summary>
    /// URL opcional da logomarca para tema claro (.png, .svg, .jpg, .jpeg, .webp).
    /// </summary>
    public string? LightLogoUrl { get; set; }

    /// <summary>
    /// URL opcional da logomarca para tema escuro (.png, .svg, .jpg, .jpeg, .webp).
    /// </summary>
    public string? DarkLogoUrl { get; set; }

    /// <summary>
    /// URL opcional do ícone favicon (.ico, .png, .svg).
    /// </summary>
    public string? FaviconUrl { get; set; }
}
