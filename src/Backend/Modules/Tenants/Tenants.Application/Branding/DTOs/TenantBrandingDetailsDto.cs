namespace Tenants.Application.Branding.DTOs;

/// <summary>
/// Data transfer object contendo os parâmetros de identidade visual e personalização White-Label do inquilino.
/// </summary>
/// <param name="PrimaryColor">Código hexadecimal da cor primária.</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária.</param>
/// <param name="LightLogoUrl">URL opcional da logomarca para fundos claros.</param>
/// <param name="DarkLogoUrl">URL opcional da logomarca para fundos escuros.</param>
/// <param name="FaviconUrl">URL opcional do ícone favicon do navegador.</param>
/// <param name="UpdatedAtUtc">Timestamp UTC da última alteração de marca.</param>
public sealed record TenantBrandingDetailsDto(
    string PrimaryColor,
    string SecondaryColor,
    string? LightLogoUrl,
    string? DarkLogoUrl,
    string? FaviconUrl,
    DateTime? UpdatedAtUtc);
