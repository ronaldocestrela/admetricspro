using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Dunning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável por operações financeiras, cobrança e régua de inadimplência (Dunning Engine).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class BillingController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BillingController"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas.</param>
    public BillingController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Dispara imediatamente um ciclo de avaliação da régua de inadimplência e bloqueio progressivo contra os tenants cadastrados.
    /// </summary>
    /// <param name="request">Parâmetros opcionais de execução contendo data de referência UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Sumário detalhado das avaliações e transições executadas.</returns>
    [HttpPost("dunning/execute")]
    [EndpointSummary("Executa o ciclo da régua de inadimplência e suspensão progressiva (Dunning Engine)")]
    [ProducesResponseType(typeof(Result<DunningExecutionSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DunningExecutionSummaryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<DunningExecutionSummaryResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<DunningExecutionSummaryResponse>>> ExecuteDunningCycle(
        [FromBody] ExecuteDunningApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new ExecuteDunningCycleCommand(request?.ReferenceDateUtc);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => UnprocessableEntity(Result<DunningExecutionSummaryResponse>.Failure(result.Error)),
                _ => BadRequest(Result<DunningExecutionSummaryResponse>.Failure(result.Error))
            };
        }

        var response = new DunningExecutionSummaryResponse(
            result.Value.EvaluatedCount,
            result.Value.TransitionsCount,
            result.Value.SuspendedCount,
            result.Value.UnchangedCount,
            result.Value.ExecutedAtUtc);

        return Ok(Result<DunningExecutionSummaryResponse>.Success(response));
    }
}
