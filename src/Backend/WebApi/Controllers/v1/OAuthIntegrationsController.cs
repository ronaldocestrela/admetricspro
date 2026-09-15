using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.Commands.HandleOAuthCallback;
using Integrations.Application.OAuth.Commands.InitiateOAuthFlow;
using Integrations.Application.OAuth.Commands.RefreshExpiringTokens;
using Integrations.Application.OAuth.Commands.RevokeOAuthConnection;
using Integrations.Application.OAuth.DTOs;
using Integrations.Application.OAuth.Queries.GetOAuthConnectionsStatus;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo Hub de Integrações OAuth2 com gerenciadores de anúncios (Meta, Google, Bing, TikTok)
/// e gerenciamento de credenciais cifradas no Token Vault.
/// </summary>
[ApiController]
[Route("api/v1/integrations/oauth")]
public sealed class OAuthIntegrationsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OAuthIntegrationsController"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas CQRS.</param>
    public OAuthIntegrationsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Inicia o fluxo OAuth gerando a URL de autorização da plataforma com state protegido contra CSRF.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace ao qual a conta será vinculada.</param>
    /// <param name="platform">Plataforma de anúncios (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="redirectUri">URI de retorno cadastrada na plataforma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>URL de redirecionamento e token de estado.</returns>
    [HttpGet("authorize-url")]
    [EndpointSummary("Inicia fluxo de autorização OAuth2 com plataforma de anúncios")]
    [ProducesResponseType(typeof(Result<OAuthAuthorizationUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OAuthAuthorizationUrlDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OAuthAuthorizationUrlDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<OAuthAuthorizationUrlDto>>> InitiateOAuthFlow(
        [FromQuery] Guid workspaceId,
        [FromQuery] string platform,
        [FromQuery] string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var command = new InitiateOAuthFlowCommand(workspaceId, platform, redirectUri);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Processa o retorno da plataforma de anúncios após consentimento do usuário, troca o código por tokens e cifra em AES-256 no vault.
    /// </summary>
    /// <param name="code">Código de autorização retornado pela rede externa.</param>
    /// <param name="state">Estado assinado anti-CSRF.</param>
    /// <param name="redirectUri">URI de retorno utilizada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Status da conexão criada e metadados operacionais.</returns>
    [HttpGet("callback")]
    [EndpointSummary("Processa o callback de autorização OAuth2 e armazena credenciais no Token Vault")]
    [ProducesResponseType(typeof(Result<OAuthConnectionStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OAuthConnectionStatusDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<OAuthConnectionStatusDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<OAuthConnectionStatusDto>>> HandleOAuthCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var command = new HandleOAuthCallbackCommand(code, state, redirectUri);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => UnprocessableEntity(result),
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Dispara renovação preventiva dos tokens com expiração próxima no banco do tenant.
    /// </summary>
    /// <param name="thresholdHours">Janela de horas de antecedência de expiração (padrão: 72 horas).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Total de conexões renovadas com sucesso.</returns>
    [HttpPost("refresh")]
    [EndpointSummary("Executa renovação preventiva de tokens de acesso prestes a expirar")]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<int>>> RefreshExpiringTokens(
        [FromQuery] int? thresholdHours = null,
        CancellationToken cancellationToken = default)
    {
        var threshold = thresholdHours.HasValue ? TimeSpan.FromHours(thresholdHours.Value) : (TimeSpan?)null;
        var command = new RefreshExpiringTokensCommand(threshold);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtém a listagem das conexões OAuth ativas e saúde dos tokens em um workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de conexões e metadados operacionais (sem tokens brutos).</returns>
    [HttpGet("status/{workspaceId:guid}")]
    [EndpointSummary("Obtém o status de todas as conexões OAuth vinculadas a um workspace")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OAuthConnectionStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<OAuthConnectionStatusDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<IReadOnlyList<OAuthConnectionStatusDto>>>> GetConnectionsStatus(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOAuthConnectionsStatusQuery(workspaceId);
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
    /// Revoga o acesso e remove/invalida as credenciais da plataforma no Token Vault do workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Plataforma de anúncios.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpDelete("{workspaceId:guid}/{platform}")]
    [EndpointSummary("Revoga uma conexão OAuth2 e desativa as credenciais no Token Vault")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result>> RevokeConnection(
        [FromRoute] Guid workspaceId,
        [FromRoute] string platform,
        CancellationToken cancellationToken = default)
    {
        var command = new RevokeOAuthConnectionCommand(workspaceId, platform);
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
