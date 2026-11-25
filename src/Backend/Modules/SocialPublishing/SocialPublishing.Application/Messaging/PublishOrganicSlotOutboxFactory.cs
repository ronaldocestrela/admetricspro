using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Messaging;

/// <summary>
/// Monta envelopes e linhas de outbox para encaminhar jobs de publicação orgânica ao broker.
/// </summary>
public static class PublishOrganicSlotOutboxFactory
{
    /// <summary>
    /// Cria a linha de outbox operacional para um job reivindicado.
    /// </summary>
    /// <param name="tenantId">Tenant do job.</param>
    /// <param name="publicationJobId">Identificador do <c>PublicationJob</c>.</param>
    /// <param name="idempotencyKey">Chave de idempotência do slot.</param>
    /// <param name="claimedAtUtc">Carimbo do claim gravado no job.</param>
    /// <param name="occurredAtUtc">Instante do encaminhamento.</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> Create(
        Guid tenantId,
        Guid publicationJobId,
        string idempotencyKey,
        DateTime claimedAtUtc,
        DateTime occurredAtUtc)
    {
        if (tenantId == Guid.Empty || publicationJobId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (claimedAtUtc.Kind != DateTimeKind.Utc || occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        var eventId = Guid.NewGuid();
        using var payloadDoc = JsonDocument.Parse(
            $$"""
            {
              "tenantId":"{{tenantId}}",
              "publicationJobId":"{{publicationJobId}}",
              "idempotencyKey":{{JsonSerializer.Serialize(idempotencyKey)}},
              "claimedAtUtc":"{{claimedAtUtc:O}}"
            }
            """);

        var envelopeResult = IntegrationEventEnvelope.Create(
            eventId,
            SocialPublishingIntegrationMessageTypes.PublishOrganicSlot,
            publicationJobId,
            new DateTimeOffset(occurredAtUtc, TimeSpan.Zero),
            IntegrationEventScope.Operational,
            tenantId,
            payloadDoc.RootElement.Clone());

        if (envelopeResult.IsFailure)
        {
            return Result<IntegrationOutboxMessage>.Failure(envelopeResult.Error);
        }

        var json = IntegrationEventEnvelopeSerializer.Serialize(envelopeResult.Value);
        return IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            eventId,
            SocialPublishingIntegrationMessageTypes.PublishOrganicSlot,
            json,
            tenantId,
            occurredAtUtc);
    }
}