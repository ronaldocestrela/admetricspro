using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Integrations.Application.Messaging;

/// <summary>
/// Monta linhas de outbox e envelopes JSON para sincronização de conta de anúncio no banco do tenant.
/// </summary>
public static class AdAccountSyncOutboxFactory
{
    /// <summary>
    /// Cria outbox para sincronização assíncrona de hierarquia e métricas de uma conta conectada.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant operacional.</param>
    /// <param name="workspaceId">Workspace da conta.</param>
    /// <param name="connectedAdAccountId">Conta conectada a sincronizar.</param>
    /// <param name="platform">Plataforma (ex.: MetaAds).</param>
    /// <param name="occurredAtUtc">Instante UTC do agendamento.</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> Create(
        Guid tenantId,
        Guid workspaceId,
        Guid connectedAdAccountId,
        string platform,
        DateTime occurredAtUtc)
    {
        if (tenantId == Guid.Empty || workspaceId == Guid.Empty || connectedAdAccountId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        var eventId = Guid.NewGuid();
        using var payloadDoc = JsonDocument.Parse(
            $$"""{"workspaceId":"{{workspaceId}}","connectedAdAccountId":"{{connectedAdAccountId}}","platform":"{{platform.Trim()}}"}""");

        var envelopeResult = IntegrationEventEnvelope.Create(
            eventId,
            IntegrationsIntegrationMessageTypes.AdAccountSync,
            connectedAdAccountId,
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
            IntegrationsIntegrationMessageTypes.AdAccountSync,
            json,
            tenantId,
            occurredAtUtc);
    }
}