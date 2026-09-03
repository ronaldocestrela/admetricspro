using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Queries.GetTenantDetails;

/// <summary>
/// Query to fetch safe tenant directory details.
/// </summary>
/// <param name="TenantId">The unique tenant identifier.</param>
public sealed record GetTenantDetailsQuery(
    TenantId TenantId) : IQuery<TenantDetailsResponse>;
