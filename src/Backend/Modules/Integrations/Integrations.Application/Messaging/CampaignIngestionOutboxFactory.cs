using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Integrations.Application.Messaging;

/// <summary>
/// Monta linhas de outbox para eventos de ingestão de campanhas no banco do tenant.
/// </summary>
public static class CampaignIngestionOutboxFactory
{
    /// <summary>
    /// Cria outbox para <c>integrations.campaign-metrics-synced.v1</c> no mesmo commit das métricas.
    /// </summary>
    /// <param name="tenantId">Tenant operacional.</param>
    /// <param name="workspaceId">Workspace da conta.</param>
    /// <param name="connectedAdAccountId">Conta conectada sincronizada.</param>
    /// <param name="platform">Plataforma de mídia.</param>
    /// <param name="granularity">Granularidade temporal ingerida.</param>
    /// <param name="windowStartUtc">Início da janela analítica (UTC).</param>
    /// <param name="windowEndUtc">Fim da janela analítica (UTC).</param>
    /// <param name="syncedAtUtc">Instante da conclusão da ingestão (UTC).</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> CreateMetricsSynced(
        Guid tenantId,
        Guid workspaceId,
        Guid connectedAdAccountId,
        string platform,
        MetricGranularity granularity,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        DateTime syncedAtUtc)
    {
        if (tenantId == Guid.Empty || workspaceId == Guid.Empty || connectedAdAccountId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (syncedAtUtc.Kind != DateTimeKind.Utc
            || windowStartUtc.Kind != DateTimeKind.Utc
            || windowEndUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        var eventId = Guid.NewGuid();
        var granularityName = granularity.ToString();
        using var payloadDoc = JsonDocument.Parse(
            $$"""
            {
              "workspaceId":"{{workspaceId}}",
              "connectedAdAccountId":"{{connectedAdAccountId}}",
              "platform":"{{platform.Trim()}}",
              "granularity":"{{granularityName}}",
              "windowStartUtc":"{{windowStartUtc.ToString("O", CultureInfo.InvariantCulture)}}",
              "windowEndUtc":"{{windowEndUtc.ToString("O", CultureInfo.InvariantCulture)}}",
              "syncedAtUtc":"{{syncedAtUtc.ToString("O", CultureInfo.InvariantCulture)}}"
            }
            """);

        return CreateOperationalLine(
            eventId,
            IntegrationsIntegrationMessageTypes.CampaignMetricsSynced,
            tenantId,
            connectedAdAccountId,
            syncedAtUtc,
            payloadDoc.RootElement.Clone());
    }

    /// <summary>
    /// Cria outbox para <c>integrations.campaign-hierarchy-synced.v1</c> no mesmo commit da hierarquia.
    /// </summary>
    /// <param name="tenantId">Tenant operacional.</param>
    /// <param name="workspaceId">Workspace da conta.</param>
    /// <param name="connectedAdAccountId">Conta conectada sincronizada.</param>
    /// <param name="platform">Plataforma de mídia.</param>
    /// <param name="syncedAtUtc">Instante da conclusão da sincronização (UTC).</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> CreateHierarchySynced(
        Guid tenantId,
        Guid workspaceId,
        Guid connectedAdAccountId,
        string platform,
        DateTime syncedAtUtc)
    {
        if (tenantId == Guid.Empty || workspaceId == Guid.Empty || connectedAdAccountId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (syncedAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        var eventId = Guid.NewGuid();
        using var payloadDoc = JsonDocument.Parse(
            $$"""
            {
              "workspaceId":"{{workspaceId}}",
              "connectedAdAccountId":"{{connectedAdAccountId}}",
              "platform":"{{platform.Trim()}}",
              "syncedAtUtc":"{{syncedAtUtc.ToString("O", CultureInfo.InvariantCulture)}}"
            }
            """);

        return CreateOperationalLine(
            eventId,
            IntegrationsIntegrationMessageTypes.CampaignHierarchySynced,
            tenantId,
            connectedAdAccountId,
            syncedAtUtc,
            payloadDoc.RootElement.Clone());
    }

    private static Result<IntegrationOutboxMessage> CreateOperationalLine(
        Guid eventId,
        string messageType,
        Guid tenantId,
        Guid correlationId,
        DateTime occurredAtUtc,
        JsonElement payload)
    {
        var envelopeResult = IntegrationEventEnvelope.Create(
            eventId,
            messageType,
            correlationId,
            new DateTimeOffset(occurredAtUtc, TimeSpan.Zero),
            IntegrationEventScope.Operational,
            tenantId,
            payload);

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