using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Integrations.Commands.ConnectDemoAdAccount;
using Tenants.Application.Integrations.DTOs;
using Tenants.Application.Integrations.Queries.GetConnectedAdAccounts;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão de integrações e contas de anúncios conectadas ao inquilino.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class IntegrationsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="IntegrationsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de comandos e consultas.</param>
    public IntegrationsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Conecta uma conta de anúncios em modo demonstração para aceleração do primeiro acesso (FTUX).
    /// </summary>
    /// <param name="request">Dados do workspace e plataforma a conectar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador da conta conectada.</returns>
    [HttpPost("demo-account")]
    [EndpointSummary("Vincula uma conta de anúncios em modo demonstração ao workspace")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<Guid>>> ConnectDemoAccount(
        [FromBody] ConnectDemoAccountApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<Guid>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new ConnectDemoAdAccountCommand(request.WorkspaceId, request.Platform);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Lista as contas de anúncios conectadas na agência, com filtro opcional por workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador opcional de workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de contas de anúncios conectadas.</returns>
    [HttpGet("connected-accounts")]
    [EndpointSummary("Lista as contas de anúncios conectadas no inquilino")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<ConnectedAdAccountDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<ConnectedAdAccountDto>>>> GetConnectedAccounts(
        [FromQuery] Guid? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetConnectedAdAccountsQuery(workspaceId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
