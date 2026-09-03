using BuildingBlocks.Application.Messaging;
using Master.Application.Integrations.DTOs;
using Master.Domain.Integrations;

namespace Master.Application.Integrations.Queries.GetTenantApiConnections;

/// <summary>
/// Query to retrieve tenant API connections with optional platform and status filtering.
/// </summary>
/// <param name="Platform">Optional platform filter.</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="PageNumber">Page number for pagination.</param>
/// <param name="PageSize">Page size limit.</param>
public sealed record GetTenantApiConnectionsQuery(
    AdPlatform? Platform = null,
    ApiConnectionStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<TenantApiConnectionDto>>;
