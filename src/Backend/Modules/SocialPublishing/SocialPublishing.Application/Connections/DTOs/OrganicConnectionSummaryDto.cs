using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Connections.DTOs;

/// <summary>
/// Metadados públicos de uma conexão orgânica (sem tokens ou ciphertext).
/// </summary>
/// <param name="ConnectionId">Identificador da conexão.</param>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="Platform">Rede orgânica.</param>
/// <param name="DisplayName">Nome de exibição da conta na rede.</param>
/// <param name="ExternalAccountId">Identificador externo da conta.</param>
/// <param name="Status">Status operacional da conexão.</param>
/// <param name="AccessTokenExpiresAtUtc">Expiração UTC do access token, se conhecida.</param>
public sealed record OrganicConnectionSummaryDto(
    Guid ConnectionId,
    Guid WorkspaceId,
    OrganicPlatform Platform,
    string DisplayName,
    string ExternalAccountId,
    OrganicConnectionStatus Status,
    DateTime? AccessTokenExpiresAtUtc);
