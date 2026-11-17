using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Traction;

/// <summary>
/// Projeção de post publicado usada pelo scan de tração orgânica.
/// </summary>
/// <param name="OrganicPostId">Identificador do post.</param>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="ConnectionId">Conexão OAuth usada na publicação.</param>
/// <param name="Platform">Rede social.</param>
/// <param name="PublishedAtUtc">Instante de publicação efetiva em UTC (slot agendado).</param>
/// <param name="PublishedExternalId">Identificador externo na rede.</param>
/// <param name="PrimaryMediaObjectKey">Chave do primeiro item de mídia, quando registrada.</param>
public sealed record OrganicPostTractionCandidate(
    Guid OrganicPostId,
    Guid WorkspaceId,
    Guid ConnectionId,
    OrganicPlatform Platform,
    DateTime PublishedAtUtc,
    string PublishedExternalId,
    string? PrimaryMediaObjectKey);
