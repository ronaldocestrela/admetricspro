using BuildingBlocks.Domain.Primitives;
using Master.Application.Users.Commands.AuthenticateBackofficeUser;
using Master.Application.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela autenticação corporativa e validação de credenciais de operadores no Backoffice.
/// </summary>
[ApiController]
[Route("api/v1/admin/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AuthController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de comandos.</param>
    public AuthController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Autentica um operador administrativo no console Backoffice.
    /// </summary>
    /// <param name="request">Credenciais de e-mail e senha.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados do operador autenticado com perfil e roles associadas.</returns>
    [HttpPost("login")]
    [EndpointSummary("Autentica um operador do Backoffice corporativo")]
    [ProducesResponseType(typeof(Result<AuthenticatedBackofficeUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthenticatedBackofficeUserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AuthenticatedBackofficeUserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<AuthenticatedBackofficeUserDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<AuthenticatedBackofficeUserDto>>> Login(
        [FromBody] BackofficeLoginApiRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Result<AuthenticatedBackofficeUserDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new AuthenticateBackofficeUserCommand(request.Email, request.Password, ipAddress);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => UnprocessableEntity(result),
                ErrorType.NotFound => Unauthorized(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}
