using BuildingBlocks.Application.Messaging;
using Integrations.Application.OAuth.DTOs;

namespace Integrations.Application.OAuth.Commands.InitiateOAuthFlow;

/// <summary>
/// Comando para iniciar o fluxo de autorização OAuth2 com uma rede externa (Meta, Google, Bing, TikTok).
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace ao qual a conexão será vinculada.</param>
/// <param name="Platform">Nome da plataforma (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
/// <param name="RedirectUri">URI de retorno configurada no SaaS.</param>
public sealed record InitiateOAuthFlowCommand(
    Guid WorkspaceId,
    string Platform,
    string RedirectUri) : ICommand<OAuthAuthorizationUrlDto>;
