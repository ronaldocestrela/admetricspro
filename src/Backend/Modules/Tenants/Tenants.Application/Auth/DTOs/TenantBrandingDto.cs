namespace Tenants.Application.Auth.DTOs;

/// <summary>
/// Dados consolidados de personalização visual e marca do inquilino (White-Label).
/// </summary>
/// <param name="AgencyName">Nome de fantasia ou razão social da agência.</param>
/// <param name="PrimaryColor">Código hexadecimal da cor primária da marca (ex: #1E40AF).</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária da marca (ex: #F59E0B).</param>
/// <param name="LightLogoUrl">URL opcional do logotipo da agência para tema claro.</param>
/// <param name="DarkLogoUrl">URL opcional do logotipo da agência para tema escuro.</param>
/// <param name="FaviconUrl">URL opcional do favicon customizado.</param>
public sealed record TenantBrandingDto(
    string AgencyName,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LightLogoUrl,
    string? DarkLogoUrl,
    string? FaviconUrl);
