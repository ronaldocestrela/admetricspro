using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Application.Status.DTOs;
using SocialPublishing.Application.Status.Queries.GetSocialModuleStatus;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
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

    /// <summary>
    /// Inicia o fluxo OAuth orgânico para Instagram, Facebook Page ou TikTok Content Posting.
    /// </summary>
    /// <param name="request">Workspace, plataforma e URI de retorno.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>URL de autorização e state anti-CSRF (sem segredos).</returns>
    [HttpPost("connections/authorize")]
    [EndpointSummary("Inicia autorização OAuth orgânica para uma rede social")]
    [ProducesResponseType(typeof(Result<StartOrganicAuthorizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<StartOrganicAuthorizationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<StartOrganicAuthorizationResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<StartOrganicAuthorizationResponse>>> AuthorizeOrganicConnection(
        [FromBody] StartOrganicAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return BadRequest(Result<StartOrganicAuthorizationResponse>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(request.WorkspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(workspaceResult),
                ErrorType.Unauthorized => Unauthorized(workspaceResult),
                _ => BadRequest(workspaceResult)
            };
        }

        var result = await _sender.Send(
            new StartOrganicAuthorizationQuery(request.WorkspaceId, request.Platform, request.RedirectUri),
            cancellationToken);

        return MapSocialConnectionResult(result);
    }

    /// <summary>
    /// Processa o callback OAuth orgânico, troca o código por tokens cifrados e persiste a conexão.
    /// </summary>
    /// <param name="request">Código, state e URI de retorno do provedor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados da conexão criada ou atualizada (sem tokens).</returns>
    [HttpPost("connections/callback")]
    [EndpointSummary("Conclui callback OAuth orgânico e grava conexão cifrada no cofre do tenant")]
    [ProducesResponseType(typeof(Result<CompleteOrganicAuthorizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CompleteOrganicAuthorizationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CompleteOrganicAuthorizationResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<CompleteOrganicAuthorizationResponse>>> CompleteOrganicAuthorization(
        [FromBody] CompleteOrganicAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return BadRequest(Result<CompleteOrganicAuthorizationResponse>.Failure(SocialErrors.WorkspaceRequired));
        }

        var workspaceResult = await _sender.Send(new GetWorkspaceByIdQuery(request.WorkspaceId), cancellationToken);
        if (workspaceResult.IsFailure)
        {
            return workspaceResult.Error.Type switch
            {
                ErrorType.NotFound => NotFound(workspaceResult),
                ErrorType.Unauthorized => Unauthorized(workspaceResult),
                _ => BadRequest(workspaceResult)
            };
        }

        var result = await _sender.Send(
            new CompleteOrganicAuthorizationCommand(
                request.WorkspaceId,
                request.Code,
                request.State,
                request.RedirectUri,
                request.ProviderError),
            cancellationToken);

        return MapSocialConnectionResult(result);
    }

    private ActionResult<Result<T>> MapSocialConnectionResult<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}

/// <summary>
/// Corpo da requisição para iniciar autorização OAuth orgânica.
/// </summary>
/// <param name="WorkspaceId">Workspace que receberá a conexão.</param>
/// <param name="Platform">Rede orgânica alvo.</param>
/// <param name="RedirectUri">URI de retorno registrada no app da rede.</param>
public sealed record StartOrganicAuthorizationRequest(
    Guid WorkspaceId,
    OrganicPlatform Platform,
    string RedirectUri);

/// <summary>
/// Corpo da requisição de callback OAuth orgânico.
/// </summary>
/// <param name="WorkspaceId">Workspace alvo da conexão.</param>
/// <param name="Code">Código de autorização.</param>
/// <param name="State">State anti-CSRF emitido no authorize.</param>
/// <param name="RedirectUri">URI de retorno utilizada no fluxo.</param>
/// <param name="ProviderError">Erro retornado pelo provedor, se houver.</param>
public sealed record CompleteOrganicAuthorizationRequest(
    Guid WorkspaceId,
    string Code,
    string State,
    string RedirectUri,
    string? ProviderError = null);
