using BuildingBlocks.Application.Messaging;
using Tenants.Application.Auth.DTOs;

namespace Tenants.Application.Auth.Queries.GetTenantPublicBranding;

/// <summary>
/// Consulta pública dos metadados de marca e identidade visual de um inquilino pelo seu subdomínio.
/// </summary>
/// <param name="Subdomain">Subdomínio a ser consultado (ex.: "vanguarda").</param>
public sealed record GetTenantPublicBrandingQuery(string Subdomain) : IQuery<TenantPublicBrandingDto>;
