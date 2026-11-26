using BuildingBlocks.Application.Messaging;
using Tenants.Application.Branding.DTOs;

namespace Tenants.Application.Branding.Queries.GetTenantBranding;

/// <summary>
/// Consulta para recuperação das configurações de identidade visual White-Label do inquilino contextual.
/// </summary>
/// <param name="RequestOrigin">Origem HTTPS da requisição para montagem de URLs de ativos hospedados.</param>
public sealed record GetTenantBrandingQuery(string? RequestOrigin = null) : IQuery<TenantBrandingDetailsDto>;
