namespace WebApi.Models;

/// <summary>
/// Modelo de requisição da API para atualização da identidade visual e White-Label do inquilino.
/// </summary>
/// <param name="PrimaryColor">Código hexadecimal da cor primária institucional (ex: #2563EB).</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária institucional (ex: #0F172A).</param>
/// <param name="LightLogoUrl">URL opcional da logomarca para tema claro (.png, .svg, .jpg, .jpeg, .webp).</param>
/// <param name="DarkLogoUrl">URL opcional da logomarca para tema escuro (.png, .svg, .jpg, .jpeg, .webp).</param>
/// <param name="FaviconUrl">URL opcional do ícone favicon do navegador (.ico, .png, .svg).</param>
public sealed record UpdateTenantBrandingApiRequest(
    string PrimaryColor,
    string SecondaryColor,
    string? LightLogoUrl = null,
    string? DarkLogoUrl = null,
    string? FaviconUrl = null);
