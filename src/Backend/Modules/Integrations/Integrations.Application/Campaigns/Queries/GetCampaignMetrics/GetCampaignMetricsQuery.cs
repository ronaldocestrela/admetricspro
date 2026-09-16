using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using Integrations.Application.Campaigns.DTOs;

namespace Integrations.Application.Campaigns.Queries.GetCampaignMetrics;

/// <summary>
/// Consulta para obter métricas analíticas normalizadas de desempenho de campanhas.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="StartDateUtc">Data inicial da janela temporal.</param>
/// <param name="EndDateUtc">Data final da janela temporal.</param>
/// <param name="Granularity">Filtro opcional por granularidade (Daily ou Hourly).</param>
/// <param name="CampaignId">Filtro opcional por campanha específica.</param>
/// <param name="ConnectedAdAccountId">Filtro opcional por conta conectada.</param>
public sealed record GetCampaignMetricsQuery(
    Guid WorkspaceId,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    MetricGranularity? Granularity = null,
    Guid? CampaignId = null,
    Guid? ConnectedAdAccountId = null) : IQuery<IReadOnlyList<CampaignMetricDto>>;
