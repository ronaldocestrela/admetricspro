using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Jobs.Queries.GetUserInitiatedJob;

namespace WebApi.Controllers.v1;

/// <summary>
/// Consulta de jobs iniciados pelo usuário no tenant.
/// </summary>
[Authorize(AuthenticationSchemes = "Tenant")]
[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/jobs")]
public sealed class JobsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Obtém o status de um job assíncrono.
    /// </summary>
    /// <param name="workspaceId">Workspace de escopo.</param>
    /// <param name="jobId">Identificador do job.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Estado Accepted, Running, Succeeded ou Failed.</returns>
    [HttpGet("{jobId:guid}")]
    [EndpointSummary("Consulta o status de um job assíncrono iniciado pelo usuário")]
    [ProducesResponseType(typeof(Result<UserInitiatedJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<UserInitiatedJobDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<UserInitiatedJobDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<UserInitiatedJobDto>>> GetJob(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUserInitiatedJobQuery(workspaceId, jobId), cancellationToken);
        if (result.IsFailure && result.Error.Type == ErrorType.NotFound)
        {
            return NotFound(result);
        }

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}