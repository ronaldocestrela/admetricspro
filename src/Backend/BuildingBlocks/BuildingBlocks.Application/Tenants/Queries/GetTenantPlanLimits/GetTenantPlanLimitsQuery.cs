using BuildingBlocks.Application.Messaging;

namespace BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;

/// <summary>
/// Consulta inter-módulos in-memory via MediatR para obter as cotas operacionais e limites do plano do inquilino.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino no catálogo Master.</param>
public sealed record GetTenantPlanLimitsQuery(Guid TenantId) : IQuery<TenantPlanLimitsDto>;
