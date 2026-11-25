using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Messaging;

/// <summary>
/// Monta linhas de outbox e envelopes JSON para eventos de integração do catálogo Master.
/// </summary>
public static class CatalogIntegrationOutboxFactory
{
    /// <summary>
    /// Cria outbox para disparo assíncrono do e-mail de boas-vindas.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant provisionado.</param>
    /// <param name="correlationId">Correlação do fluxo (tipicamente o id do tenant).</param>
    /// <param name="occurredAtUtc">Instante UTC do fato.</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> CreateWelcomeEmailOutbox(
        Guid tenantId,
        Guid correlationId,
        DateTime occurredAtUtc) =>
        CreateCatalogOutbox(CatalogIntegrationMessageTypes.TenantWelcomeEmail, tenantId, correlationId, occurredAtUtc);

    /// <summary>
    /// Cria outbox para confirmação de assinatura paga.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant ativado.</param>
    /// <param name="correlationId">Correlação do fluxo.</param>
    /// <param name="occurredAtUtc">Instante UTC da ativação.</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> CreateSubscriptionConfirmationOutbox(
        Guid tenantId,
        Guid correlationId,
        DateTime occurredAtUtc) =>
        CreateCatalogOutbox(
            CatalogIntegrationMessageTypes.SubscriptionConfirmationEmail,
            tenantId,
            correlationId,
            occurredAtUtc);

    private static Result<IntegrationOutboxMessage> CreateCatalogOutbox(
        string messageType,
        Guid tenantId,
        Guid correlationId,
        DateTime occurredAtUtc)
    {
        if (tenantId == Guid.Empty || correlationId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        var eventId = Guid.NewGuid();
        using var payloadDoc = JsonDocument.Parse($$"""{"tenantId":"{{tenantId}}"}""");
        var envelopeResult = IntegrationEventEnvelope.Create(
            eventId,
            messageType,
            correlationId,
            new DateTimeOffset(occurredAtUtc, TimeSpan.Zero),
            IntegrationEventScope.Catalog,
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
            messageType,
            json,
            tenantId,
            occurredAtUtc);
    }
}