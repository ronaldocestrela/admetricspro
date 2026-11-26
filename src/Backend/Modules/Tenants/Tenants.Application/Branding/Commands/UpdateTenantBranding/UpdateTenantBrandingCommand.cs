using BuildingBlocks.Application.Messaging;
using Tenants.Application.Branding.DTOs;

namespace Tenants.Application.Branding.Commands.UpdateTenantBranding;

/// <summary>
/// Comando para atualização das configurações visuais de White-Label do inquilino.
/// </summary>
/// <param name="PrimaryColor">Código hexadecimal da cor primária.</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária.</param>
/// <param name="LightLogoUrl">URL opcional da logomarca para fundos claros (.png, .svg, .jpg, .jpeg, .webp).</param>
/// <param name="DarkLogoUrl">URL opcional da logomarca para fundos escuros (.png, .svg, .jpg, .jpeg, .webp).</param>
/// <param name="FaviconUrl">URL opcional do favicon (.ico, .png, .svg).</param>
/// <param name="RequestOrigin">Origem HTTPS da requisição para sincronização de URLs de objeto.</param>
public sealed record UpdateTenantBrandingCommand(
    string PrimaryColor,
    string SecondaryColor,
    string? LightLogoUrl,
    string? DarkLogoUrl,
    string? FaviconUrl,
    string? RequestOrigin = null) : ICommand<TenantBrandingDetailsDto>;
