using Automations.Application.SafetyGuards.Commands.CheckLandingPages;
using Automations.Application.SafetyGuards.Commands.CheckOverspending;
using Automations.Application.SafetyGuards.DTOs;
using Automations.Application.SafetyGuards.Queries.ListSafetyIncidents;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelas travas de segurança operacional (Overspending e Detector de links 404/500).
/// </summary>
[ApiController]
[Route("api/v1/automations/guards")]
public sealed class SecurityGuardsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SecurityGuardsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory MediatR.</param>
    public SecurityGuardsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Executa a auditoria de consumo e aciona a trava de Overspending para o workspace.
    /// Pausa imediatamente campanhas cujo gasto diário supere 120% do orçamento configurado e despacha alarmes multi-canal.
    /// </summary>
    /// <param name="request">Parâmetros de execução contendo o workspace e limiar de estouro.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da auditoria de overspending e incidentes gerados.</returns>
    [HttpPost("overspending/check")]
    [EndpointSummary("Executa a trava de segurança de Overspending (>120% do orçamento diário)")]
    [ProducesResponseType(typeof(Result<CheckOverspendingResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CheckOverspendingResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<CheckOverspendingResultDto>>> CheckOverspending(
        [FromBody] CheckOverspendingApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CheckOverspendingCommand(
            request.WorkspaceId,
            request.ThresholdMultiplier);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Executa o teste de integridade HTTP (Detector 404/500) nas URLs finais de anúncios ativos.
    /// Pausa preventivamente anúncios cujo destino retorne erro HTTP 4xx, 5xx ou inacessibilidade.
    /// </summary>
    /// <param name="request">Parâmetros de execução contendo o workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da auditoria de URLs e incidentes gerados.</returns>
    [HttpPost("landing-pages/check")]
    [EndpointSummary("Executa a verificação de saúde das Landing Pages (Detector 404/500)")]
    [ProducesResponseType(typeof(Result<CheckLandingPagesResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CheckLandingPagesResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<CheckLandingPagesResultDto>>> CheckLandingPages(
        [FromBody] CheckLandingPagesApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CheckLandingPagesCommand(request.WorkspaceId);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtém o histórico de incidentes e travas de segurança acionadas para o workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="limit">Quantidade máxima de incidentes a retornar (padrão: 50).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de incidentes de segurança registrados.</returns>
    [HttpGet("incidents")]
    [EndpointSummary("Lista os incidentes de travas de segurança acionadas no workspace")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<SafetyIncidentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<SafetyIncidentDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<IReadOnlyList<SafetyIncidentDto>>>> ListIncidents(
        [FromQuery] Guid workspaceId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new ListSafetyIncidentsQuery(workspaceId, limit);
        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
