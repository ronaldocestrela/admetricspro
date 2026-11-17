using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Users.Commands.AcceptTenantUserInvitation;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Queries.GetTenantInvitationByToken;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Endpoints públicos para aceite de convites de colaboradores.
/// </summary>
[ApiController]
[Route("api/v1/public/tenant-invitations")]
[AllowAnonymous]
public sealed class PublicTenantInvitationsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PublicTenantInvitationsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory.</param>
    public PublicTenantInvitationsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Consulta os metadados públicos de um convite pelo token e subdomínio.
    /// </summary>
    /// <param name="token">Token opaco do convite.</param>
    /// <param name="subdomain">Subdomínio da agência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pré-visualização do convite.</returns>
    [HttpGet("{token}")]
    [EndpointSummary("Consulta pública de convite de colaborador por token")]
    [ProducesResponseType(typeof(Result<TenantInvitationPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantInvitationPreviewDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<TenantInvitationPreviewDto>>> GetInvitation(
        [FromRoute] string token,
        [FromQuery] string subdomain,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTenantInvitationByTokenQuery(token, subdomain), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Aceita o convite e define a senha do colaborador.
    /// </summary>
    /// <param name="request">Token, subdomínio e credenciais.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost("accept")]
    [EndpointSummary("Aceita convite de colaborador e define senha")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result>> AcceptInvitation(
        [FromBody] AcceptTenantInvitationApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new AcceptTenantUserInvitationCommand(
            request.Token,
            request.Subdomain,
            request.Password,
            request.ConfirmPassword);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Conflict => Conflict(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}