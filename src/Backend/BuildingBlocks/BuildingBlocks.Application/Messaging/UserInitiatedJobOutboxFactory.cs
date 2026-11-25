using System.Text.Json;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Monta linhas de outbox para executar jobs iniciados pelo usuário.
/// </summary>
public static class UserInitiatedJobOutboxFactory
{
    /// <summary>
    /// Cria outbox operacional para executar um job aceito.
    /// </summary>
    /// <param name="tenantId">Tenant do job.</param>
    /// <param name="jobId">Identificador do job.</param>
    /// <param name="eventId">EventId alinhado ao job.</param>
    /// <param name="messageType">Tipo versionado da mensagem.</param>
    /// <param name="occurredAtUtc">Instante da gravação.</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> Create(
        Guid tenantId,
        Guid jobId,
        Guid eventId,
        string messageType,
        DateTime occurredAtUtc)
    {
        if (tenantId == Guid.Empty || jobId == Guid.Empty || eventId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        using var payloadDoc = JsonDocument.Parse(
            $$"""
            {
              "tenantId":"{{tenantId}}",
              "jobId":"{{jobId}}"
            }
            """);

        var envelopeResult = IntegrationEventEnvelope.Create(
            eventId,
            messageType.Trim(),
            jobId,
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
            messageType.Trim(),
            json,
            tenantId,
            occurredAtUtc);
    }
}