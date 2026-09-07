using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Workspaces.Commands.CreateWorkspace;
using Tenants.Application.Workspaces.Commands.ToggleWorkspaceStatus;
using Tenants.Application.Workspaces.Commands.UpdateWorkspace;
using Tenants.Application.Workspaces.DTOs;
using Tenants.Application.Workspaces.Queries.GetWorkspaceById;
using Tenants.Application.Workspaces.Queries.GetWorkspaces;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão de clientes da agência (Workspaces) no banco de dados dedicado do inquilino.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class WorkspacesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="WorkspacesController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory de comandos e consultas.</param>
    public WorkspacesController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Cadastra um novo cliente/workspace no ambiente isolado da agência, validando as cotas do plano contratado.
    /// </summary>
    /// <param name="request">Dados de entrada do novo workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do workspace criado ou falha semântica.</returns>
    [HttpPost]
    [EndpointSummary("Cadastra um novo cliente/workspace na agência com validação de cotas")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result<Guid>>> CreateWorkspace(
        [FromBody] CreateWorkspaceApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<Guid>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new CreateWorkspaceCommand(
            request.Name,
            request.CnpjOrCpf,
            request.MonthlyAdSpendBudget,
            request.Segment);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Conflict => Conflict(result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return CreatedAtAction(nameof(GetWorkspaceById), new { id = result.Value }, result);
    }

    /// <summary>
    /// Lista os workspaces cadastrados no inquilino com filtro opcional por status ativo.
    /// </summary>
    /// <param name="activeOnly">Filtro opcional para retornar apenas workspaces ativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista com os dados dos workspaces.</returns>
    [HttpGet]
    [EndpointSummary("Lista os clientes/workspaces da agência")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<WorkspaceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<WorkspaceDto>>>> GetWorkspaces(
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkspacesQuery(activeOnly);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém os dados detalhados de um cliente/workspace específico.
    /// </summary>
    /// <param name="id">Identificador único do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados do workspace ou 404 caso não exista.</returns>
    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtém os detalhes de um cliente/workspace pelo identificador")]
    [ProducesResponseType(typeof(Result<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<WorkspaceDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<WorkspaceDto>>> GetWorkspaceById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkspaceByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Atualiza os dados cadastrais, documento fiscal e orçamento de um workspace existente.
    /// </summary>
    /// <param name="id">Identificador único do workspace a ser atualizado.</param>
    /// <param name="request">Novos dados cadastrais.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Atualiza os dados cadastrais de um workspace")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result>> UpdateWorkspace(
        [FromRoute] Guid id,
        [FromBody] UpdateWorkspaceApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new UpdateWorkspaceCommand(
            id,
            request.Name,
            request.CnpjOrCpf,
            request.MonthlyAdSpendBudget,
            request.Segment);

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
    /// Alterna o status operacional de um workspace entre ativo e inativo, validando cotas ao reativar.
    /// </summary>
    /// <param name="id">Identificador único do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPatch("{id:guid}/toggle-status")]
    [EndpointSummary("Alterna o status de um workspace (ativo/inativo)")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> ToggleWorkspaceStatus(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ToggleWorkspaceStatusCommand(id);
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
}
