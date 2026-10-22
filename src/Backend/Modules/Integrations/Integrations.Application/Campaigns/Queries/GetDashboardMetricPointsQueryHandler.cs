using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns;
using MediatR;

namespace Integrations.Application.Campaigns.Queries;

/// <summary>Exporta métricas diárias do repositório do tenant sem gerar dados sintéticos.</summary>
/// <param name="sender">Mediador de autorização de carteira.</param>
/// <param name="metrics">Repositório de métricas de Integrations.</param>
public sealed class GetDashboardMetricPointsQueryHandler(ISender sender, ICampaignMetricsRepository metrics)
    : IQueryHandler<GetDashboardMetricPointsQuery, IReadOnlyList<DashboardMetricPoint>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DashboardMetricPoint>>> Handle(GetDashboardMetricPointsQuery request, CancellationToken cancellationToken)
    {
        if (request.StartDateUtc > request.EndDateUtc)
            return Result<IReadOnlyList<DashboardMetricPoint>>.Failure(Error.Validation("Dashboard.InvalidDateRange", "Período inválido."));
        var workspaces = await sender.Send(new GetAnalyticsWorkspaceIdsQuery(request.WorkspaceId), cancellationToken);
        if (workspaces.IsFailure) return Result<IReadOnlyList<DashboardMetricPoint>>.Failure(workspaces.Error);
        var points = new List<DashboardMetricPoint>();
        foreach (var workspaceId in workspaces.Value.Distinct())
        {
            var result = await metrics.GetMetricsAsync(workspaceId, request.StartDateUtc, request.EndDateUtc,
                MetricGranularity.Daily, cancellationToken: cancellationToken);
            if (result.IsFailure) return Result<IReadOnlyList<DashboardMetricPoint>>.Failure(result.Error);
            points.AddRange(result.Value.Select(m => new DashboardMetricPoint(m.Date, m.Platform, m.Currency,
                m.Spend, m.ConversionValue, m.Impressions, m.Clicks, m.Conversions)));
        }
        return Result<IReadOnlyList<DashboardMetricPoint>>.Success(points);
    }
}
