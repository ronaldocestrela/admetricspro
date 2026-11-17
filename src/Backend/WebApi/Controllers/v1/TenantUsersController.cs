using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Users.Commands.InviteTenantUser;
using Tenants.Application.Users.Commands.ResendTenantUserInvitation;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Queries.GetTenantUsers;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo gerenciamento de colaboradores e membros da agência no banco dedicado do inquilino.
/// </summary>
[ApiController]
[Route("api/v1/tenants/users")]
public sealed class TenantUsersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantUsersController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de comandos e consultas.</param>
    public TenantUsersController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Cadastra ou convida um novo colaborador para a agência.
    /// </summary>
    /// <param name="request">Dados cadastrais do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do colaborador criado.</returns>
    [HttpPost]
    [EndpointSummary("Cadastra ou convida um novo membro/colaborador para a agência")]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<InviteTenantUserResultDto>>> InviteUser(
        [FromBody] InviteTenantUserApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<InviteTenantUserResultDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new InviteTenantUserCommand(
            request.FullName,
            request.Email,
            request.Role,
            request.PhoneNumber);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Reenvia o e-mail de convite para um colaborador com conta pendente.
    /// </summary>
    /// <param name="id">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado do reenvio com status do e-mail.</returns>
    [HttpPost("{id:guid}/invitations")]
    [EndpointSummary("Reenvia convite por e-mail para colaborador pendente")]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<InviteTenantUserResultDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Result<InviteTenantUserResultDto>>> ResendInvitation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ResendTenantUserInvitationCommand(id);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Lista os colaboradores da agência com filtro opcional por status ativo.
    /// </summary>
    /// <param name="activeOnly">Filtro opcional por status ativo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de colaboradores.</returns>
    [HttpGet]
    [EndpointSummary("Lista os colaboradores da agência no inquilino")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<TenantUserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<TenantUserDto>>>> GetUsers(
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTenantUsersQuery(activeOnly);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
