using Analytics.Application.Copilot.Commands.ExecuteCopilotRecommendation;
using Analytics.Application.Copilot.DTOs;
using Analytics.Application.Copilot.Queries.GetDailyDiagnostic;
using Analytics.Domain.Copilot;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo Copiloto de Otimização via IA (Auditor de Tráfego - Subfase 5.3).
/// Expõe diagnósticos de anomalias (overlap no Meta e canibalização Google vs Bing) e execução de recomendações em 1 clique.
/// </summary>
[ApiController]
[Route("api/v1/analytics/copilot")]
public sealed class CopilotController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CopilotController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory MediatR.</param>
    public CopilotController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Recupera o diagnóstico diário sintetizado em texto natural e o catálogo de anomalias identificadas no workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador único do workspace.</param>
    /// <param name="date">Data de referência do diagnóstico (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Relatório de diagnóstico diário estruturado.</returns>
    [HttpGet("diagnostic")]
    [EndpointSummary("Recupera o diagnóstico diário sintetizado e anomalias de tráfego do workspace")]
    [ProducesResponseType(typeof(Result<DailyDiagnosticReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DailyDiagnosticReportDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<DailyDiagnosticReportDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<DailyDiagnosticReportDto>>> GetDiagnostic(
        [FromQuery] Guid workspaceId,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetDailyDiagnosticQuery(workspaceId, date);
            var result = await _sender.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<DailyDiagnosticReportDto>.Failure(
                    Error.Failure("Copilot.InternalError", $"Falha interna ao consultar diagnóstico do Copiloto: {ex.Message}")));
        }
    }

    /// <summary>
    /// Executa operacionalmente em 1 clique uma ação recomendada pelo Copiloto (pausar conjunto, negativar palavra-chave, etc.).
    /// </summary>
    /// <param name="request">Payload com a identificação da ação e parâmetros operacionais.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução contendo confirmação e detalhes de auditoria.</returns>
    [HttpPost("actions/execute")]
    [EndpointSummary("Executa uma recomendação de otimização do Copiloto em 1 clique")]
    [ProducesResponseType(typeof(Result<ExecuteCopilotActionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExecuteCopilotActionResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ExecuteCopilotActionResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Result<ExecuteCopilotActionResultDto>>> ExecuteAction(
        [FromBody] ExecuteCopilotActionApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request is null)
            {
                return BadRequest(Result<ExecuteCopilotActionResultDto>.Failure(
                    Error.Validation("Copilot.NullRequest", "O corpo da requisição não pode ser nulo.")));
            }

            if (!Enum.TryParse<CopilotActionType>(request.ActionType, true, out var actionType))
            {
                actionType = CopilotActionType.PauseAdSet;
            }

            var command = new ExecuteCopilotRecommendationCommand(
                WorkspaceId: request.WorkspaceId,
                ActionId: request.ActionId,
                ActionType: actionType,
                TargetEntityId: request.TargetEntityId,
                TargetEntityName: request.TargetEntityName,
                Platform: request.Platform,
                Title: request.Title,
                Description: request.Description,
                Parameters: request.Parameters
            );

            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                Result<ExecuteCopilotActionResultDto>.Failure(
                    Error.Failure("Copilot.InternalError", $"Falha interna ao despachar ação do Copiloto: {ex.Message}")));
        }
    }
}
