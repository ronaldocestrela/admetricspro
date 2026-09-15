using BuildingBlocks.Application.Messaging;
using Integrations.Application.OAuth.DTOs;

namespace Integrations.Application.OAuth.Commands.HandleOAuthCallback;

/// <summary>
/// Comando que processa o callback retornado pelo provedor OAuth2 com code e state.
/// </summary>
/// <param name="Code">Código de autorização retornado pelo provedor externo.</param>
/// <param name="State">Estado anti-CSRF gerado no início do fluxo.</param>
/// <param name="RedirectUri">URI de redirecionamento utilizada na autorização.</param>
public sealed record HandleOAuthCallbackCommand(
    string Code,
    string State,
    string RedirectUri) : ICommand<OAuthConnectionStatusDto>;
