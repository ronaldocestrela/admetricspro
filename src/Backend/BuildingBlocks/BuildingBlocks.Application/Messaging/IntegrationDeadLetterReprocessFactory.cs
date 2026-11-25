using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Cria uma nova linha de outbox a partir de uma mensagem em dead-letter.
/// </summary>
public static class IntegrationDeadLetterReprocessFactory
{
    /// <summary>
    /// Gera uma linha <see cref="IntegrationOutboxMessageStatus.Pending"/> com novo <c>EventId</c> e <c>correlationId</c> apontando para o evento original.
    /// </summary>
    /// <param name="deadLetter">Linha em estado morto.</param>
    /// <param name="utcNow">Instante de criação.</param>
    /// <returns>Nova linha ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> CreateReprocessLine(
        IntegrationOutboxMessage deadLetter,
        DateTime utcNow)
    {
        if (deadLetter.Status != IntegrationOutboxMessageStatus.DeadLetter)
        {
            return Result<IntegrationOutboxMessage>.Failure(
                Error.Validation("Messaging.NotDeadLetter", "Somente linhas em dead-letter podem ser reprocessadas."));
        }

        var scope = deadLetter.TenantId is null
            ? IntegrationEventScope.Catalog
            : IntegrationEventScope.Operational;

        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(deadLetter.PayloadJson, scope);
        if (envelopeResult.IsFailure)
        {
            return Result<IntegrationOutboxMessage>.Failure(envelopeResult.Error);
        }

        var original = envelopeResult.Value;
        var newEventId = Guid.NewGuid();
        var recreated = IntegrationEventEnvelope.Create(
            newEventId,
            original.MessageType,
            original.EventId,
            original.OccurredOnUtc,
            original.Scope,
            original.TenantId,
            original.Payload);

        if (recreated.IsFailure)
        {
            return Result<IntegrationOutboxMessage>.Failure(recreated.Error);
        }

        var payloadJson = IntegrationEventEnvelopeSerializer.Serialize(recreated.Value);
        return IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            newEventId,
            deadLetter.MessageType,
            payloadJson,
            deadLetter.TenantId,
            utcNow);
    }
}