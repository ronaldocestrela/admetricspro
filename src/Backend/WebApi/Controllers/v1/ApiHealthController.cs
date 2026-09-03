using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.Commands.RecordApiConsumption;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Queries.GetApiHealthOverview;
using Master.Application.Integrations.Queries.GetTenantApiConnections;
using Master.Domain.Integrations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo monitoramento de saúde de APIs de mídia,
/// rastreamento de rate limits com alertas preventivos (80%) e governança de tokens de inquilinos.
/// </summary>
[ApiController]
[Route("api/v1/admin/api-health")]
public sealed class ApiHealthController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ApiHealthController"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas.</param>
    public ApiHealthController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Obtém o resumo consolidado de cotas de APIs e saúde de conexões de inquilinos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Visão geral de saúde das APIs e cotas em tempo real.</returns>
    [HttpGet]
    [EndpointSummary("Obtém o resumo operacional consolidado de cotas de APIs e conexões")]
    [ProducesResponseType(typeof(Result<ApiHealthOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<ApiHealthOverviewDto>>> GetOverview(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetApiHealthOverviewQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lista conexões de APIs de inquilinos com suporte a filtros de plataforma e saúde do token.
    /// </summary>
    /// <param name="platform">Filtro opcional por plataforma.</param>
    /// <param name="status">Filtro opcional por status do token.</param>
    /// <param name="pageNumber">Número da página.</param>
    /// <param name="pageSize">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista paginada de conexões de APIs.</returns>
    [HttpGet("connections")]
    [EndpointSummary("Lista conexões de APIs de inquilinos com filtros")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<TenantApiConnectionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<TenantApiConnectionDto>>>> GetConnections(
        [FromQuery] AdPlatform? platform = null,
        [FromQuery] ApiConnectionStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetTenantApiConnectionsQuery(platform, status, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Registra unidades consumidas de API de uma rede de anúncios.
    /// </summary>
    /// <param name="request">Dados de consumo da operação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Status de cota atualizado com nível de alerta.</returns>
    [HttpPost("usage")]
    [EndpointSummary("Registra consumo de operações contra a cota de uma API")]
    [ProducesResponseType(typeof(Result<PlatformQuotaStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PlatformQuotaStatusDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<PlatformQuotaStatusDto>>> RecordUsage(
        [FromBody] RecordUsageApiRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _sender.Send(
            new RecordApiConsumptionCommand(request.Platform, request.Units, request.TimestampUtc),
            cancellationToken);

        if (result.IsFailure)
        {
            return UnprocessableEntity(result);
        }

        return Ok(result);
    }
}
