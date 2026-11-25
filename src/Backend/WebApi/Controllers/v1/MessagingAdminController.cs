using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Messaging.Commands.ReprocessIntegrationDeadLetter;
using Master.Application.Messaging.Queries.GetIntegrationDeadLetters;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Operações administrativas de mensageria durável (dead-letter) para o Backoffice.
/// </summary>
[ApiController]
[Route("api/v1/admin/messaging")]
public sealed class MessagingAdminController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lista dead-letters do catálogo e dos tenants elegíveis.
    /// </summary>
    /// <param name="limit">Quantidade máxima aproximada.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linhas mortas.</returns>
    [HttpGet("dead-letters")]
    [EndpointSummary("Lista dead-letters de integração")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>>> GetDeadLetters(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetIntegrationDeadLettersQuery(limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Republica uma dead-letter criando nova linha de outbox.
    /// </summary>
    /// <param name="eventId">Evento morto.</param>
    /// <param name="request">Contexto de tenant quando aplicável.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Novo EventId da tentativa.</returns>
    [HttpPost("dead-letters/{eventId:guid}/reprocess")]
    [EndpointSummary("Reprocessa uma dead-letter de integração")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<Guid>>> ReprocessDeadLetter(
        Guid eventId,
        [FromBody] ReprocessDeadLetterApiRequest? request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ReprocessIntegrationDeadLetterCommand(eventId, request?.TenantId),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Conflict => Conflict(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result),
            };
        }

        return Ok(result);
    }
}