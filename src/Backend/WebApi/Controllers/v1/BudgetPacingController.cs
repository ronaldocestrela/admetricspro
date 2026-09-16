using Automations.Application.Pacing.DTOs;
using Automations.Application.Pacing.Queries.GetWorkspaceBudgetPacing;
using Automations.Application.Pacing.Queries.ListPortfolioBudgetPacing;
using Automations.Application.Pacing.Queries.SimulateBudgetPacing;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão dinâmica de orçamentos, cálculo de pacing e previsão de fim de mês.
/// </summary>
[ApiController]
[Route("api/v1/automations/pacing")]
public sealed class BudgetPacingController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BudgetPacingController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory MediatR.</param>
    public BudgetPacingController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Obtém a análise de pacing e projeção de encerramento do mês para um Workspace específico.
    /// </summary>
    /// <param name="workspaceId">Identificador único do Workspace.</param>
    /// <param name="year">Ano de referência (opcional, padrão: ano corrente).</param>
    /// <param name="month">Mês de referência (1 a 12, opcional, padrão: mês corrente).</param>
    /// <param name="asOfDateUtc">Data de corte para o cálculo (opcional, padrão: data atual UTC).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados consolidados de pacing e detalhamento por campanha.</returns>
    [HttpGet("workspaces/{workspaceId:guid}")]
    [EndpointSummary("Obtém o pacing e previsão de fim de mês do workspace")]
    [ProducesResponseType(typeof(Result<WorkspaceBudgetPacingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<WorkspaceBudgetPacingDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<WorkspaceBudgetPacingDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<WorkspaceBudgetPacingDto>>> GetWorkspacePacing(
        [FromRoute] Guid workspaceId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] DateTime? asOfDateUtc,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkspaceBudgetPacingQuery(workspaceId, year, month, asOfDateUtc);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtém a visão executiva consolidada de pacing de toda a carteira de clientes do inquilino.
    /// </summary>
    /// <param name="squadId">Filtro opcional por Squad.</param>
    /// <param name="year">Ano de referência (opcional, padrão: ano corrente).</param>
    /// <param name="month">Mês de referência (1 a 12, opcional, padrão: mês corrente).</param>
    /// <param name="asOfDateUtc">Data de corte para o cálculo (opcional, padrão: data atual UTC).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sumário executivo de pacing da carteira e contagem de status.</returns>
    [HttpGet("portfolio")]
    [EndpointSummary("Consolida o pacing de toda a carteira de clientes")]
    [ProducesResponseType(typeof(Result<PortfolioPacingSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PortfolioPacingSummaryDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<PortfolioPacingSummaryDto>>> GetPortfolioPacing(
        [FromQuery] Guid? squadId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] DateTime? asOfDateUtc,
        CancellationToken cancellationToken)
    {
        var query = new ListPortfolioBudgetPacingQuery(squadId, year, month, asOfDateUtc);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Simula cenários hipotéticos de velocidade de consumo e projeção de encerramento do ciclo sob demanda.
    /// </summary>
    /// <param name="request">Parâmetros da simulação contendo orçamento, gasto e intervalo temporal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado simulado de pacing e status.</returns>
    [HttpPost("simulate")]
    [EndpointSummary("Simula cenários hipotéticos de pacing sob demanda")]
    [ProducesResponseType(typeof(Result<WorkspaceBudgetPacingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<WorkspaceBudgetPacingDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<WorkspaceBudgetPacingDto>>> SimulatePacing(
        [FromBody] SimulateBudgetPacingApiRequest request,
        CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDateUtc ?? DateTime.UtcNow;
        var query = new SimulateBudgetPacingQuery(
            request.TargetBudget,
            request.CurrentSpend,
            request.StartDateUtc,
            request.EndDateUtc,
            asOf,
            request.TolerancePercentage);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
