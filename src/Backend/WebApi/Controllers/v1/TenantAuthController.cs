using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Auth.Commands.AuthenticateTenantUser;
using Tenants.Application.Auth.DTOs;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela autenticação e emissão de tokens de acesso para usuários de agências e inquilinos.
/// </summary>
[ApiController]
[Route("api/v1/tenants/auth")]
public sealed class TenantAuthController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantAuthController"/>.
    /// </summary>
    /// <param name="sender">Instância do mediador in-memory para envio de comandos.</param>
    public TenantAuthController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Autentica um usuário ou gestor de agência (inquilino) e retorna o token de acesso JWT da sessão.
    /// </summary>
    /// <param name="request">Credenciais de e-mail, senha e subdomínio opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento assíncrono.</param>
    /// <returns>Dados da sessão autenticada com token JWT e metadados visuais de marca.</returns>
    [HttpPost("login")]
    [EndpointSummary("Autentica um usuário de inquilino (gestor ou colaborador de agência)")]
    [ProducesResponseType(typeof(Result<AuthenticatedTenantUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthenticatedTenantUserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<AuthenticatedTenantUserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<AuthenticatedTenantUserDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<AuthenticatedTenantUserDto>>> Login(
        [FromBody] TenantLoginApiRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Result<AuthenticatedTenantUserDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new AuthenticateTenantUserCommand(
            request.Email,
            request.Password,
            request.Subdomain,
            TenantId: null,
            ipAddress);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => UnprocessableEntity(result),
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.NotFound => Unauthorized(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}
