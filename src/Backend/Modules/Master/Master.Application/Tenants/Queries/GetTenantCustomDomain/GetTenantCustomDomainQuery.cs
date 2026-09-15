using BuildingBlocks.Application.Messaging;

namespace Master.Application.Tenants.Queries.GetTenantCustomDomain;

/// <summary>
/// Consulta para obter a configuração e status do domínio customizado CNAME de um inquilino.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino.</param>
public sealed record GetTenantCustomDomainQuery(Guid TenantId) : IQuery<TenantCustomDomainDto>;
