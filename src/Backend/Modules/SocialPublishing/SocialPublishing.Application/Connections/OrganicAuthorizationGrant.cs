namespace SocialPublishing.Application.Connections;

/// <summary>
/// Resultado da troca OAuth orgânica com metadados da conta publicável (sem persistência).
/// </summary>
/// <param name="AccessToken">Access token em claro (somente no escopo da troca; nunca expor na API).</param>
/// <param name="RefreshToken">Refresh token em claro opcional.</param>
/// <param name="ExpiresAtUtc">Expiração UTC do access token.</param>
/// <param name="ExternalAccountId">Identificador externo da conta publicável.</param>
/// <param name="DisplayName">Nome de exibição da conta.</param>
/// <param name="Scopes">Escopos concedidos (formato persistido no agregado).</param>
public sealed record OrganicAuthorizationGrant(
    string AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAtUtc,
    string ExternalAccountId,
    string DisplayName,
    string Scopes);
