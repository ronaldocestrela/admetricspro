using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Squads.Commands.AddSquadMember;
using Tenants.Application.Squads.Commands.AssignSquadWorkspace;
using Tenants.Application.Squads.Commands.CreateSquad;
using Tenants.Application.Squads.Commands.RemoveSquadMember;
using Tenants.Application.Squads.Commands.ToggleSquadStatus;
using Tenants.Application.Squads.Commands.UnassignSquadWorkspace;
using Tenants.Application.Squads.Commands.UpdateSquad;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Squads.Queries.GetSquadById;
using Tenants.Application.Squads.Queries.GetSquads;
using Tenants.Application.Squads.Queries.GetUserAccessibleWorkspaces;
using Tenants.Application.Workspaces.DTOs;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão de equipes internas (Squads), alocação de membros e governança de carteira de clientes.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class SquadsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SquadsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de comandos e consultas.</param>
    public SquadsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Cadastra um novo squad no ambiente operacional da agência.
    /// </summary>
    /// <param name="request">Dados de identificação e escopo do squad.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Identificador do squad criado ou falha semântica.</returns>
    [HttpPost]
    [EndpointSummary("Cadastra um novo time/squad interno na agência")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result<Guid>>> CreateSquad(
        [FromBody] CreateSquadApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<Guid>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new CreateSquadCommand(request.Name, request.Description);
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Conflict => Conflict(result),
                _ => BadRequest(result)
            };
        }

        return CreatedAtAction(nameof(GetSquadById), new { id = result.Value }, result);
    }

    /// <summary>
    /// Lista os squads cadastrados na agência com suas contagens de membros e clientes.
    /// </summary>
    /// <param name="activeOnly">Filtro opcional para retornar apenas squads ativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista com o resumo de cada squad.</returns>
    [HttpGet]
    [EndpointSummary("Lista os squads da agência com métricas de membros e clientes")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<SquadSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<SquadSummaryDto>>>> GetSquads(
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSquadsQuery(activeOnly);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém os dados detalhados de um squad específico, incluindo membros e clientes alocados.
    /// </summary>
    /// <param name="id">Identificador único do squad.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados completos do squad ou 404 caso não exista.</returns>
    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtém os detalhes completos de um squad")]
    [ProducesResponseType(typeof(Result<SquadDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SquadDetailsDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<SquadDetailsDto>>> GetSquadById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSquadByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);
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
    /// Atualiza as informações cadastrais de um squad (nome e descrição).
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="request">Novos dados cadastrais.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Atualiza os dados de um squad")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result>> UpdateSquad(
        Guid id,
        [FromBody] UpdateSquadApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new UpdateSquadCommand(id, request.Name, request.Description);
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

    /// <summary>
    /// Altera o status operacional do squad (ativo ou inativo).
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="request">Novo status.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPatch("{id:guid}/status")]
    [EndpointSummary("Alterna o status operacional do squad")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> ToggleSquadStatus(
        Guid id,
        [FromBody] ToggleSquadStatusApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new ToggleSquadStatusCommand(id, request.IsActive);
        var result = await _sender.Send(command, cancellationToken);
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
    /// Vincula um colaborador como membro da equipe do squad.
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="request">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da vinculação.</returns>
    [HttpPost("{id:guid}/members")]
    [EndpointSummary("Adiciona um colaborador ao squad")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result>> AddSquadMember(
        Guid id,
        [FromBody] AddSquadMemberApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new AddSquadMemberCommand(id, request.UserId);
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

    /// <summary>
    /// Desvincula um colaborador da equipe do squad.
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da desvinculação.</returns>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [EndpointSummary("Remove um colaborador do squad")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> RemoveSquadMember(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new RemoveSquadMemberCommand(id, userId);
        var result = await _sender.Send(command, cancellationToken);
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
    /// Aloca um cliente/workspace à carteira de atendimento do squad.
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="request">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da alocação.</returns>
    [HttpPost("{id:guid}/workspaces")]
    [EndpointSummary("Aloca um cliente/workspace à carteira do squad")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result>> AssignSquadWorkspace(
        Guid id,
        [FromBody] AssignSquadWorkspaceApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new AssignSquadWorkspaceCommand(id, request.WorkspaceId);
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

    /// <summary>
    /// Remove um cliente/workspace da carteira de atendimento do squad.
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da desassociação.</returns>
    [HttpDelete("{id:guid}/workspaces/{workspaceId:guid}")]
    [EndpointSummary("Desassocia um cliente/workspace da carteira do squad")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> UnassignSquadWorkspace(
        Guid id,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var command = new UnassignSquadWorkspaceCommand(id, workspaceId);
        var result = await _sender.Send(command, cancellationToken);
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
    /// Consulta a carteira consolidada de clientes/workspaces acessíveis a um colaborador conforme as regras de isolamento.
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de workspaces acessíveis pelo colaborador.</returns>
    [HttpGet("users/{userId:guid}/portfolio")]
    [EndpointSummary("Obtém os clientes acessíveis a um colaborador segundo regras de isolamento por carteira")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<WorkspaceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<WorkspaceDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<IReadOnlyList<WorkspaceDto>>>> GetUserPortfolio(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserAccessibleWorkspacesQuery(userId);
        var result = await _sender.Send(query, cancellationToken);
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
}
