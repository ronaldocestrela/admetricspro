using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Campaigns.Queries.GetCampaignMetrics;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela ingestão analítica e consulta de métricas diárias e horárias de campanhas
/// das redes de tráfego pago (Meta Ads, Google Ads, TikTok Ads e Bing Ads).
/// </summary>
[ApiController]
[Route("api/v1/integrations/campaigns/metrics")]
public sealed class CampaignMetricsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CampaignMetricsController"/>.
    /// </summary>
    /// <param name="sender">Mediador CQRS.</param>
    public CampaignMetricsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Dispara o pipeline de ingestão de métricas analíticas diárias e horárias de campanhas.
    /// </summary>
    /// <param name="request">Filtros de workspace, conta, datas e granularidade temporal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resumo executivo de métricas processadas com idempotência garantida.</returns>
    [HttpPost("sync")]
    [EndpointSummary("Dispara a ingestão de métricas diárias e horárias de campanhas de um workspace")]
    [ProducesResponseType(typeof(Result<SyncCampaignMetricsSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SyncCampaignMetricsSummaryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<SyncCampaignMetricsSummaryDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<SyncCampaignMetricsSummaryDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<SyncCampaignMetricsSummaryDto>>> SyncMetrics(
        [FromBody] SyncCampaignMetricsApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<SyncCampaignMetricsSummaryDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var granularity = string.Equals(request.Granularity, "Hourly", StringComparison.OrdinalIgnoreCase)
            ? MetricGranularity.Hourly
            : MetricGranularity.Daily;

        var end = request.EndDateUtc ?? DateTime.UtcNow;
        var start = request.StartDateUtc ?? end.AddDays(-7);

        var command = new SyncCampaignMetricsCommand(
            request.WorkspaceId,
            request.ConnectedAdAccountId,
            request.Platform,
            start,
            end,
            granularity);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Unauthorized => Unauthorized(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Consulta métricas analíticas de desempenho de campanhas com KPIs derivados calculados.
    /// </summary>
    /// <param name="workspaceId">Identificador obrigatório do workspace.</param>
    /// <param name="startDateUtc">Data inicial (UTC).</param>
    /// <param name="endDateUtc">Data final (UTC).</param>
    /// <param name="granularity">Granularidade temporal ("Daily" ou "Hourly").</param>
    /// <param name="campaignId">Filtro opcional por campanha específica.</param>
    /// <param name="connectedAdAccountId">Filtro opcional por conta conectada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de métricas normalizadas.</returns>
    [HttpGet]
    [EndpointSummary("Consulta métricas analíticas de desempenho de campanhas por período e granularidade")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<CampaignMetricDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<CampaignMetricDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<IReadOnlyList<CampaignMetricDto>>>> GetMetrics(
        [FromQuery] Guid workspaceId,
        [FromQuery] DateTime? startDateUtc = null,
        [FromQuery] DateTime? endDateUtc = null,
        [FromQuery] string? granularity = null,
        [FromQuery] Guid? campaignId = null,
        [FromQuery] Guid? connectedAdAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var end = endDateUtc ?? DateTime.UtcNow;
        var start = startDateUtc ?? end.AddDays(-30);

        MetricGranularity? parsedGranularity = null;
        if (!string.IsNullOrWhiteSpace(granularity))
        {
            if (Enum.TryParse<MetricGranularity>(granularity, true, out var g))
            {
                parsedGranularity = g;
            }
        }

        var query = new GetCampaignMetricsQuery(
            workspaceId,
            start,
            end,
            parsedGranularity,
            campaignId,
            connectedAdAccountId);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
