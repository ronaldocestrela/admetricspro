namespace Integrations.Application.OAuth.DTOs;

/// <summary>
/// DTO contendo a URL de autorização gerada e o token de estado para proteção anti-CSRF.
/// </summary>
/// <param name="AuthorizationUrl">URL para a qual o usuário deve ser redirecionado para autorização.</param>
/// <param name="State">Valor do estado anti-CSRF assinado.</param>
public sealed record OAuthAuthorizationUrlDto(string AuthorizationUrl, string State);
