using System.Text.Json;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Envelope versionado publicado no exchange de integração após commit na outbox.
/// </summary>
public sealed record IntegrationEventEnvelope : IIntegrationEvent
{
    private IntegrationEventEnvelope(
        Guid eventId,
        string messageType,
        Guid correlationId,
        DateTimeOffset occurredOnUtc,
        IntegrationEventScope scope,
        Guid? tenantId,
        JsonElement payload)
    {
        EventId = eventId;
        MessageType = messageType;
        CorrelationId = correlationId;
        OccurredOnUtc = occurredOnUtc;
        Scope = scope;
        TenantId = tenantId;
        Payload = payload;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public string MessageType { get; }

    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; }

    /// <inheritdoc />
    public IntegrationEventScope Scope { get; }

    /// <inheritdoc />
    public Guid? TenantId { get; }

    /// <summary>
    /// Obtém o payload mínimo do evento como objeto JSON.
    /// </summary>
    public JsonElement Payload { get; }

    /// <summary>
    /// Valida e cria um envelope de integração.
    /// </summary>
    /// <param name="eventId">Identificador único do evento.</param>
    /// <param name="messageType">Tipo versionado da mensagem.</param>
    /// <param name="correlationId">Correlação do fluxo causador.</param>
    /// <param name="occurredOnUtc">Instante UTC do fato.</param>
    /// <param name="scope">Escopo catálogo ou operacional.</param>
    /// <param name="tenantId">Tenant contextual quando exigido ou conhecido.</param>
    /// <param name="payload">Corpo JSON; deve ser um objeto.</param>
    /// <returns>Envelope válido ou erro de negócio sem exceção.</returns>
    public static Result<IntegrationEventEnvelope> Create(
        Guid eventId,
        string messageType,
        Guid correlationId,
        DateTimeOffset occurredOnUtc,
        IntegrationEventScope scope,
        Guid? tenantId,
        JsonElement payload)
    {
        if (eventId == Guid.Empty || correlationId == Guid.Empty)
        {
            return Result<IntegrationEventEnvelope>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            return Result<IntegrationEventEnvelope>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (payload.ValueKind != JsonValueKind.Object)
        {
            return Result<IntegrationEventEnvelope>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (scope == IntegrationEventScope.Operational && tenantId is null)
        {
            return Result<IntegrationEventEnvelope>.Failure(MessagingErrors.TenantRequired);
        }

        var envelope = new IntegrationEventEnvelope(
            eventId,
            messageType.Trim(),
            correlationId,
            occurredOnUtc,
            scope,
            tenantId,
            payload.Clone());

        return Result<IntegrationEventEnvelope>.Success(envelope);
    }
}