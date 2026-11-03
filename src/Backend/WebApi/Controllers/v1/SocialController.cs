using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SocialPublishing.Application.Status.DTOs;
using SocialPublishing.Application.Status.Queries.GetSocialModuleStatus;
using Tenants.Application.Workspaces.Queries.GetWorkspaceById;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador HTTP do bounded context Social Publishing (publicação orgânica e calendário).
/// </summary>
[Authorize(AuthenticationSchemes = "Tenant")]
[ApiController]
[Route("api/v1/social")]
public sealed class SocialController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory MediatR.</param>
    public SocialController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Obtém o resumo operacional do módulo (conexões orgânicas e posts agendados) para o workspace informado.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contagens agregadas ou falha de negócio/autorização.</returns>
    [HttpGet("status")]
    [EndpointSummary("Obtém o resumo operacional do módulo Social Publishing para um workspace")]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result<SocialModuleStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<SocialModuleStatusDto>>> GetStatus(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return BadRequest(Result<SocialModuleStatusDto>.Failure(
                SocialPublishing.Domain.SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(workspaceResult),
                ErrorType.Unauthorized => Unauthorized(workspaceResult),
                _ => BadRequest(workspaceResult)
            };
        }

        var result = await _sender.Send(new GetSocialModuleStatusQuery(workspaceId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}
