using BuildingBlocks.Application.Messaging;
using Master.Application.Tenants.Queries.GetTenantDetails;

namespace Master.Application.Tenants.Queries.GetTenants;

/// <summary>
/// Consulta para listagem do catálogo de todos os inquilinos registrados.
/// </summary>
public sealed record GetTenantsQuery : IQuery<IReadOnlyList<TenantDetailsResponse>>;
