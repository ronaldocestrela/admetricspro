using BuildingBlocks.Application.Messaging;
using Integrations.Application.OAuth.DTOs;

namespace Integrations.Application.OAuth.Queries.GetOAuthConnectionsStatus;

/// <summary>
/// Consulta para obter todas as conexões OAuth ativas e status dos tokens de um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace.</param>
public sealed record GetOAuthConnectionsStatusQuery(
    Guid WorkspaceId) : IQuery<IReadOnlyList<OAuthConnectionStatusDto>>;
