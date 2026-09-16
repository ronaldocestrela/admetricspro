using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Domain.Campaigns;

namespace Integrations.Application.Campaigns.Queries.GetCampaignMetrics;

/// <summary>
/// Manipulador da consulta <see cref="GetCampaignMetricsQuery"/>.
/// Recupera métricas consolidadas do repositório multitenant e calcula KPIs derivados para visualização.
/// </summary>
public sealed class GetCampaignMetricsQueryHandler : IQueryHandler<GetCampaignMetricsQuery, IReadOnlyList<CampaignMetricDto>>
{
    private readonly ICampaignMetricsRepository _metricsRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetCampaignMetricsQueryHandler"/>.
    /// </summary>
    /// <param name="metricsRepository">Repositório de métricas.</param>
    public GetCampaignMetricsQueryHandler(ICampaignMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository ?? throw new ArgumentNullException(nameof(metricsRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CampaignMetricDto>>> Handle(
        GetCampaignMetricsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<CampaignMetricDto>>.Failure(
                Error.Validation("GetCampaignMetrics.EmptyWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        if (request.StartDateUtc > request.EndDateUtc)
        {
            return Result<IReadOnlyList<CampaignMetricDto>>.Failure(
                Error.Validation("GetCampaignMetrics.InvalidDateRange", "A data inicial não pode ser superior à data final."));
        }

        var result = await _metricsRepository.GetMetricsAsync(
            request.WorkspaceId,
            request.StartDateUtc,
            request.EndDateUtc,
            request.Granularity,
            request.CampaignId,
            request.ConnectedAdAccountId,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<IReadOnlyList<CampaignMetricDto>>.Failure(result.Error);
        }

        var dtos = result.Value.Select(m => new CampaignMetricDto(
            Id: m.Id,
            WorkspaceId: m.WorkspaceId,
            ConnectedAdAccountId: m.ConnectedAdAccountId,
            CampaignId: m.CampaignId,
            AdSetId: m.AdSetId,
            AdId: m.AdId,
            Platform: m.Platform,
            ExternalCampaignId: m.ExternalCampaignId,
            ExternalAdSetId: m.ExternalAdSetId,
            ExternalAdId: m.ExternalAdId,
            Date: m.Date,
            Hour: m.Hour,
            Granularity: m.Granularity.ToString(),
            Spend: m.Spend,
            Currency: m.Currency,
            Impressions: m.Impressions,
            Clicks: m.Clicks,
            Conversions: m.Conversions,
            ConversionValue: m.ConversionValue,
            Ctr: m.Ctr,
            Cpc: m.Cpc,
            Cpm: m.Cpm,
            Cpa: m.Cpa,
            Roas: m.Roas,
            SyncedAtUtc: m.SyncedAtUtc)).ToList();

        return Result<IReadOnlyList<CampaignMetricDto>>.Success(dtos);
    }
}
