using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Registro de idempotência de transporte por consumidor e evento.
/// </summary>
public sealed class IntegrationInboxMessage : Entity<Guid>
{
    private IntegrationInboxMessage(
        Guid id,
        Guid eventId,
        string consumerName,
        DateTime processedAtUtc)
        : base(id)
    {
        EventId = eventId;
        ConsumerName = consumerName;
        ProcessedAtUtc = processedAtUtc;
    }

    private IntegrationInboxMessage()
        : base(Guid.Empty)
    {
        ConsumerName = string.Empty;
    }

    /// <summary>
    /// Obtém o identificador do evento processado.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Obtém o nome estável do consumidor (handler).
    /// </summary>
    public string ConsumerName { get; private set; }

    /// <summary>
    /// Obtém o instante UTC em que o processamento foi concluído com ack.
    /// </summary>
    public DateTime ProcessedAtUtc { get; private set; }

    /// <summary>
    /// Cria um registro de inbox após processamento bem-sucedido ou ack permanente.
    /// </summary>
    /// <param name="id">Identificador da linha.</param>
    /// <param name="eventId">Identificador do evento.</param>
    /// <param name="consumerName">Nome do consumidor.</param>
    /// <param name="processedAtUtc">Instante de processamento em UTC.</param>
    /// <returns>Registro criado ou falha de validação.</returns>
    public static Result<IntegrationInboxMessage> Create(
        Guid id,
        Guid eventId,
        string consumerName,
        DateTime processedAtUtc)
    {
        if (eventId == Guid.Empty)
        {
            return Result<IntegrationInboxMessage>.Failure(IntegrationInboxErrors.InvalidEventId);
        }

        if (string.IsNullOrWhiteSpace(consumerName))
        {
            return Result<IntegrationInboxMessage>.Failure(IntegrationInboxErrors.InvalidConsumerName);
        }

        if (processedAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationInboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        return Result<IntegrationInboxMessage>.Success(
            new IntegrationInboxMessage(id, eventId, consumerName.Trim(), processedAtUtc));
    }
}