using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.OAuth;

/// <summary>
/// Estrutura de dados contendo o resultado da troca ou renovação de credenciais OAuth2 com o provedor.
/// </summary>
/// <param name="AccessToken">Access token obtido do provedor.</param>
/// <param name="RefreshToken">Refresh token opcional para provedores que o suportam.</param>
/// <param name="AccessTokenExpiresAtUtc">Data/hora UTC de expiração do token de acesso.</param>
/// <param name="RefreshTokenExpiresAtUtc">Data/hora UTC de expiração do refresh token.</param>
/// <param name="Scopes">Escopos efetivamente concedidos pelo usuário/provedor.</param>
/// <param name="ExternalAccountId">Identificador da conta de anúncios obtido no fluxo, se disponível.</param>
/// <param name="ExternalAccountName">Nome amigável da conta de anúncios obtido no fluxo, se disponível.</param>
public sealed record OAuthTokenResult(
    string AccessToken,
    string? RefreshToken,
    DateTime? AccessTokenExpiresAtUtc,
    DateTime? RefreshTokenExpiresAtUtc,
    string Scopes,
    string? ExternalAccountId = null,
    string? ExternalAccountName = null);
