using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Master.Application.Messaging.Commands.ReprocessIntegrationDeadLetter;

/// <summary>
/// Republica uma dead-letter como nova linha de outbox.
/// </summary>
/// <param name="EventId">Evento morto.</param>
/// <param name="TenantId">Tenant quando a linha é operacional; nulo no catálogo.</param>
public sealed record ReprocessIntegrationDeadLetterCommand(Guid EventId, Guid? TenantId)
    : IRequest<Result<Guid>>;