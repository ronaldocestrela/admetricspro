using BuildingBlocks.Application.Messaging;
using Tenants.Application.Branding.DTOs;

namespace Tenants.Application.Branding.Queries.GetTenantBranding;

/// <summary>
/// Consulta para recuperação das configurações de identidade visual White-Label do inquilino contextual.
/// </summary>
public sealed record GetTenantBrandingQuery : IQuery<TenantBrandingDetailsDto>;
