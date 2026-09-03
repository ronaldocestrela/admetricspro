using BuildingBlocks.Domain.Primitives;
using Master.Application.Plans.Commands.CreatePlan;
using Master.Application.Plans.Commands.UpdatePlan;
using Master.Application.Plans.DTOs;
using Master.Application.Plans.Queries.GetPlanById;
using Master.Application.Plans.Queries.GetPlans;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela governança de planos de assinatura e tiers comerciais no MasterDb.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class PlansController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PlansController"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas.</param>
    public PlansController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Lista todos os planos de assinatura cadastrados no catálogo master.
    /// </summary>
    /// <param name="includeInactive">Se verdadeiro, inclui planos inativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de planos parametrizados.</returns>
    [HttpGet]
    [EndpointSummary("Lista os planos de assinatura e tiers disponíveis")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<PlanDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<PlanDto>>>> GetPlans(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPlansQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém os detalhes de um plano de assinatura pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador único do plano.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados do plano ou erro 404 se não localizado.</returns>
    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtém um plano de assinatura pelo ID")]
    [ProducesResponseType(typeof(Result<PlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PlanDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<PlanDto>>> GetPlanById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPlanByIdQuery(id), cancellationToken);
        if (result.Value is null)
        {
            return NotFound(Result<PlanDto>.Failure(Error.NotFound("Plan.NotFound", $"Plano com ID '{id}' não foi localizado.")));
        }

        return Ok(Result<PlanDto>.Success(result.Value));
    }

    /// <summary>
    /// Cria um novo plano de assinatura com limites estruturais e flags de recursos.
    /// </summary>
    /// <param name="request">Dados de entrada do plano.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do plano criado ou falha de negócio/validação.</returns>
    [HttpPost]
    [EndpointSummary("Cadastra um novo plano comercial de assinatura")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<Guid>>> CreatePlan(
        [FromBody] CreatePlanApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreatePlanCommand(
            request.Name,
            request.Description,
            request.Tier,
            request.MonthlyPrice,
            request.AnnualDiscountPercentage,
            request.MaxSeats,
            request.MaxWorkspaces,
            request.MonthlyAdSpendCap,
            request.HasWhiteLabel,
            request.HasCustomCname,
            request.HasAiCopilot,
            request.HasCrossNetworkAutomations);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(result),
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return CreatedAtAction(nameof(GetPlanById), new { id = result.Value.Value }, Result<Guid>.Success(result.Value.Value));
    }

    /// <summary>
    /// Atualiza os parâmetros, cotas e features de um plano de assinatura existente.
    /// </summary>
    /// <param name="id">Identificador único do plano a ser atualizado.</param>
    /// <param name="request">Novos parâmetros do plano.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Atualiza cotas, features e precificação de um plano")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result>> UpdatePlan(
        Guid id,
        [FromBody] UpdatePlanApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdatePlanCommand(
            id,
            request.Name,
            request.Description,
            request.MonthlyPrice,
            request.AnnualDiscountPercentage,
            request.MaxSeats,
            request.MaxWorkspaces,
            request.MonthlyAdSpendCap,
            request.HasWhiteLabel,
            request.HasCustomCname,
            request.HasAiCopilot,
            request.HasCrossNetworkAutomations);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(result),
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}
