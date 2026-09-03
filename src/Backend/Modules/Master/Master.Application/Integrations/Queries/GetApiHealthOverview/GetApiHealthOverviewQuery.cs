using BuildingBlocks.Application.Messaging;
using Master.Application.Integrations.DTOs;

namespace Master.Application.Integrations.Queries.GetApiHealthOverview;

/// <summary>
/// Query to obtain the consolidated API health summary, rate limit quotas, and tenant connection metrics.
/// </summary>
public sealed record GetApiHealthOverviewQuery : IQuery<ApiHealthOverviewDto>;
