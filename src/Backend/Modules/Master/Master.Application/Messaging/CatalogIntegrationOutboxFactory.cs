using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Tenants;

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

    /// <summary>
    /// Cria outbox para um marco da régua de trial.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="noticeType">Marco pendente (7, 3, 1 dia ou expirado).</param>
    /// <param name="correlationId">Correlação do fluxo.</param>
    /// <param name="occurredAtUtc">Instante UTC da avaliação.</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> CreateTrialNoticeOutbox(
        Guid tenantId,
        TrialNoticeType noticeType,
        Guid correlationId,
        DateTime occurredAtUtc)
    {
        var eventId = CatalogIntegrationStableEventIds.ForTrialNotice(tenantId, noticeType);
        var noticeName = noticeType.ToString();
        using var payloadDoc = JsonDocument.Parse(
            $$"""{"tenantId":"{{tenantId}}","noticeType":"{{noticeName}}"}""");

        return CreateCatalogOutbox(
            CatalogIntegrationMessageTypes.TrialNoticeEmail,
            tenantId,
            correlationId,
            occurredAtUtc,
            eventId,
            payloadDoc.RootElement.Clone());
    }

    /// <summary>
    /// Cria outbox para notificação de transição de estágio de inadimplência.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="dunningStage">Novo estágio (D+3, D+7 ou D+14).</param>
    /// <param name="paymentDueDateUtc">Vencimento da fatura em aberto.</param>
    /// <param name="correlationId">Correlação do fluxo.</param>
    /// <param name="occurredAtUtc">Instante UTC da transição.</param>
    /// <returns>Linha de outbox ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> CreateDunningStageOutbox(
        Guid tenantId,
        DunningStage dunningStage,
        DateTime paymentDueDateUtc,
        Guid correlationId,
        DateTime occurredAtUtc)
    {
        if (paymentDueDateUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        var eventId = CatalogIntegrationStableEventIds.ForDunningStage(tenantId, dunningStage, paymentDueDateUtc);
        var dueIso = paymentDueDateUtc.ToString("O", CultureInfo.InvariantCulture);
        var stageName = dunningStage.ToString();
        using var payloadDoc = JsonDocument.Parse(
            $$"""{"tenantId":"{{tenantId}}","dunningStage":"{{stageName}}","paymentDueDateUtc":"{{dueIso}}"}""");

        return CreateCatalogOutbox(
            CatalogIntegrationMessageTypes.DunningStageEmail,
            tenantId,
            correlationId,
            occurredAtUtc,
            eventId,
            payloadDoc.RootElement.Clone());
    }

    private static Result<IntegrationOutboxMessage> CreateCatalogOutbox(
        string messageType,
        Guid tenantId,
        Guid correlationId,
        DateTime occurredAtUtc,
        Guid? eventId = null,
        JsonElement? payload = null)
    {
        if (tenantId == Guid.Empty || correlationId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        var resolvedEventId = eventId ?? Guid.NewGuid();
        using var payloadDoc = payload is null
            ? JsonDocument.Parse($$"""{"tenantId":"{{tenantId}}"}""")
            : null;
        var payloadElement = payload ?? payloadDoc!.RootElement.Clone();

        var envelopeResult = IntegrationEventEnvelope.Create(
            resolvedEventId,
            messageType,
            correlationId,
            new DateTimeOffset(occurredAtUtc, TimeSpan.Zero),
            IntegrationEventScope.Catalog,
            tenantId,
            payloadElement.Clone());

        if (envelopeResult.IsFailure)
        {
            return Result<IntegrationOutboxMessage>.Failure(envelopeResult.Error);
        }

        var json = IntegrationEventEnvelopeSerializer.Serialize(envelopeResult.Value);
        return IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            resolvedEventId,
            messageType,
            json,
            tenantId,
            occurredAtUtc);
    }
}