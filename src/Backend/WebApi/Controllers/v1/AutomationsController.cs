using Automations.Application.Rules.Commands.CreateRule;
using Automations.Application.Rules.Commands.DeleteRule;
using Automations.Application.Rules.Commands.EvaluateRule;
using Automations.Application.Rules.Commands.ToggleRuleState;
using Automations.Application.Rules.Commands.UpdateRule;
using Automations.Application.Rules.DTOs;
using Automations.Application.Rules.Queries.GetRuleById;
using Automations.Application.Rules.Queries.ListRulesByWorkspace;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão do construtor de regras cross-platform (DSL) e automações operacionais.
/// </summary>
[ApiController]
[Route("api/v1/automations/rules")]
public sealed class AutomationsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AutomationsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory MediatR.</param>
    public AutomationsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Cria uma nova regra de automação com árvore de predicados DSL e ações de mutação.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Cria uma nova regra de automação cross-platform")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<Guid>>> CreateRule(
        [FromBody] CreateRuleApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRuleCommand(
            request.WorkspaceId,
            request.Name,
            request.Description,
            request.ConditionTree,
            request.Actions,
            request.IsEnabled);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetRuleById), new { id = result.Value, workspaceId = request.WorkspaceId }, result);
    }

    /// <summary>
    /// Obtém todas as regras de automação cadastradas para um workspace.
    /// </summary>
    [HttpGet]
    [EndpointSummary("Lista todas as regras de automação do workspace")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<AutomationRuleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<AutomationRuleDto>>>> ListRules(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var query = new ListRulesByWorkspaceQuery(workspaceId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém os detalhes de uma regra de automação pelo seu identificador.
    /// </summary>
    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtém uma regra de automação específica")]
    [ProducesResponseType(typeof(Result<AutomationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AutomationRuleDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<AutomationRuleDto>>> GetRuleById(
        [FromRoute] Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var query = new GetRuleByIdQuery(workspaceId, id);
        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Atualiza os parâmetros, árvore de condições ou ações de uma regra de automação.
    /// </summary>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Atualiza uma regra de automação")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> UpdateRule(
        [FromRoute] Guid id,
        [FromBody] UpdateRuleApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRuleCommand(
            request.WorkspaceId,
            id,
            request.Name,
            request.Description,
            request.ConditionTree,
            request.Actions);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Alterna o status ativo/inativo de uma regra de automação.
    /// </summary>
    [HttpPatch("{id:guid}/toggle")]
    [EndpointSummary("Ativa ou desativa uma regra de automação")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> ToggleRule(
        [FromRoute] Guid id,
        [FromBody] ToggleRuleApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ToggleRuleStateCommand(request.WorkspaceId, id, request.IsEnabled);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Exclui uma regra de automação existente.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [EndpointSummary("Exclui uma regra de automação")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> DeleteRule(
        [FromRoute] Guid id,
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRuleCommand(workspaceId, id);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Força a avaliação imediata de uma regra de automação e despacha mutações se os gatilhos forem satisfeitos.
    /// </summary>
    [HttpPost("{id:guid}/evaluate")]
    [EndpointSummary("Avalia e executa uma regra de automação")]
    [ProducesResponseType(typeof(Result<RuleEvaluationReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RuleEvaluationReportDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<RuleEvaluationReportDto>>> EvaluateRule(
        [FromRoute] Guid id,
        [FromBody] EvaluateRuleApiRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EvaluateRuleCommand(request.WorkspaceId, id);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
