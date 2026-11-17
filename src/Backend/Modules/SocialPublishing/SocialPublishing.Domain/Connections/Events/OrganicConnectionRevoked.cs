using BuildingBlocks.Domain.Abstractions;

namespace SocialPublishing.Domain.Connections.Events;

/// <summary>
/// Evento de domínio emitido quando uma conexão orgânica é revogada.
/// </summary>
/// <param name="ConnectionId">Identificador da conexão revogada.</param>
/// <param name="WorkspaceId">Identificador do workspace proprietário.</param>
public sealed record OrganicConnectionRevoked(Guid ConnectionId, Guid WorkspaceId) : IDomainEvent;
