using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;

/// <summary>
/// Resposta HTTP do callback OAuth orgânico (sem segredos).
/// </summary>
/// <param name="ConnectionId">Identificador da conexão persistida ou atualizada.</param>
/// <param name="Platform">Plataforma orgânica conectada.</param>
/// <param name="DisplayName">Nome de exibição da conta publicável.</param>
/// <param name="AccessTokenExpiresAtUtc">Expiração UTC do access token.</param>
public sealed record CompleteOrganicAuthorizationResponse(
    Guid ConnectionId,
    OrganicPlatform Platform,
    string DisplayName,
    DateTime? AccessTokenExpiresAtUtc);
