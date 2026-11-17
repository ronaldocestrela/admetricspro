using BuildingBlocks.Application.Messaging;
using Tenants.Application.Users.DTOs;

namespace Tenants.Application.Users.Queries.GetTenantInvitationByToken;

/// <summary>
/// Consulta pública de metadados de um convite pelo token e subdomínio.
/// </summary>
/// <param name="Token">Token opaco.</param>
/// <param name="Subdomain">Subdomínio da agência.</param>
public sealed record GetTenantInvitationByTokenQuery(string Token, string Subdomain)
    : IQuery<TenantInvitationPreviewDto>;