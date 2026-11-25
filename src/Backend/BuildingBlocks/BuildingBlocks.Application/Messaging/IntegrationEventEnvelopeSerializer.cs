using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Serializa e desserializa <see cref="IntegrationEventEnvelope"/> com nomes JSON estáveis.
/// </summary>
public static class IntegrationEventEnvelopeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializa o envelope para JSON com propriedades em camelCase explícitas.
    /// </summary>
    /// <param name="envelope">Envelope validado.</param>
    /// <returns>Representação JSON do envelope.</returns>
    public static string Serialize(IntegrationEventEnvelope envelope)
    {
        var dto = IntegrationEventEnvelopeDto.FromEnvelope(envelope);
        return JsonSerializer.Serialize(dto, Options);
    }

    /// <summary>
    /// Desserializa JSON em envelope validado.
    /// </summary>
    /// <param name="json">Texto JSON do envelope.</param>
    /// <param name="scope">Escopo usado quando o JSON não inclui escopo (padrão operacional se houver tenant).</param>
    /// <returns>Envelope válido ou erro de validação.</returns>
    public static Result<IntegrationEventEnvelope> Deserialize(string json, IntegrationEventScope? scope = null)
    {
        IntegrationEventEnvelopeDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<IntegrationEventEnvelopeDto>(json, Options);
        }
        catch (JsonException)
        {
            return Result<IntegrationEventEnvelope>.Failure(MessagingErrors.PayloadInvalid);
        }

        if (dto is null)
        {
            return Result<IntegrationEventEnvelope>.Failure(MessagingErrors.PayloadInvalid);
        }

        var resolvedScope = scope ?? (dto.TenantId.HasValue
            ? IntegrationEventScope.Operational
            : IntegrationEventScope.Catalog);

        if (dto.TenantId is null && resolvedScope == IntegrationEventScope.Operational)
        {
            return Result<IntegrationEventEnvelope>.Failure(MessagingErrors.TenantRequired);
        }

        return IntegrationEventEnvelope.Create(
            dto.EventId,
            dto.MessageType ?? string.Empty,
            dto.CorrelationId,
            dto.OccurredOnUtc,
            resolvedScope,
            dto.TenantId,
            dto.Payload);
    }

    private sealed class IntegrationEventEnvelopeDto
    {
        [JsonPropertyName("eventId")]
        public Guid EventId { get; init; }

        [JsonPropertyName("messageType")]
        public string? MessageType { get; init; }

        [JsonPropertyName("correlationId")]
        public Guid CorrelationId { get; init; }

        [JsonPropertyName("occurredOnUtc")]
        public DateTimeOffset OccurredOnUtc { get; init; }

        [JsonPropertyName("tenantId")]
        public Guid? TenantId { get; init; }

        [JsonPropertyName("payload")]
        public JsonElement Payload { get; init; }

        public static IntegrationEventEnvelopeDto FromEnvelope(IntegrationEventEnvelope envelope) =>
            new()
            {
                EventId = envelope.EventId,
                MessageType = envelope.MessageType,
                CorrelationId = envelope.CorrelationId,
                OccurredOnUtc = envelope.OccurredOnUtc,
                TenantId = envelope.TenantId,
                Payload = envelope.Payload,
            };
    }
}