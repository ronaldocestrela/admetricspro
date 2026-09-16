using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using Integrations.Application.Campaigns.DTOs;

namespace Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;

/// <summary>
/// Comando para disparar o pipeline de ingestão de métricas diárias e horárias de desempenho de campanhas.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace do inquilino.</param>
/// <param name="ConnectedAdAccountId">Filtro opcional para sincronizar apenas uma conta específica.</param>
/// <param name="Platform">Filtro opcional para plataforma de anúncios.</param>
/// <param name="StartDateUtc">Data inicial da janela de sincronização.</param>
/// <param name="EndDateUtc">Data final da janela de sincronização.</param>
/// <param name="Granularity">Granularidade temporal (Daily ou Hourly).</param>
public sealed record SyncCampaignMetricsCommand(
    Guid WorkspaceId,
    Guid? ConnectedAdAccountId,
    string? Platform,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    MetricGranularity Granularity) : ICommand<SyncCampaignMetricsSummaryDto>;
