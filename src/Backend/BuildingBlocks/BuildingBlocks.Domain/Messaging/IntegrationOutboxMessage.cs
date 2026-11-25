using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Linha de outbox transacional que representa um evento de integração pendente de publicação no broker.
/// </summary>
public sealed class IntegrationOutboxMessage : Entity<Guid>
{
    private IntegrationOutboxMessage(
        Guid id,
        Guid eventId,
        string messageType,
        string payloadJson,
        Guid? tenantId,
        IntegrationOutboxMessageStatus status,
        int attemptCount,
        DateTime? nextAttemptAtUtc,
        byte[] rowVersion)
        : base(id)
    {
        EventId = eventId;
        MessageType = messageType;
        PayloadJson = payloadJson;
        TenantId = tenantId;
        Status = status;
        AttemptCount = attemptCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        RowVersion = rowVersion;
    }

    private IntegrationOutboxMessage()
        : base(Guid.Empty)
    {
        MessageType = string.Empty;
        PayloadJson = string.Empty;
        RowVersion = Array.Empty<byte>();
    }

    /// <summary>
    /// Obtém o identificador único do evento usado para idempotência e cabeçalho <c>message-id</c>.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Obtém o tipo versionado da mensagem (ex.: <c>integrations.campaign-metrics-synced.v1</c>).
    /// </summary>
    public string MessageType { get; private set; }

    /// <summary>
    /// Obtém o payload serializado em JSON do envelope.
    /// </summary>
    public string PayloadJson { get; private set; }

    /// <summary>
    /// Obtém o tenant contextual quando o evento é operacional; nulo para eventos de catálogo puros.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Obtém o status atual da linha na outbox.
    /// </summary>
    public IntegrationOutboxMessageStatus Status { get; private set; }

    /// <summary>
    /// Obtém a quantidade de tentativas de publicação já realizadas.
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Obtém o próximo instante UTC em que a linha pode ser despachada, quando em backoff.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; private set; }

    /// <summary>
    /// Obtém o token de concorrência otimista para claim exclusivo do dispatcher.
    /// </summary>
    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Cria uma nova linha de outbox em estado <see cref="IntegrationOutboxMessageStatus.Pending"/>.
    /// </summary>
    /// <param name="id">Identificador da linha de outbox.</param>
    /// <param name="eventId">Identificador único do evento.</param>
    /// <param name="messageType">Tipo versionado da mensagem.</param>
    /// <param name="payloadJson">Payload JSON do envelope.</param>
    /// <param name="tenantId">Tenant opcional para eventos operacionais ou de catálogo com contexto.</param>
    /// <returns>A linha criada ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> Create(
        Guid id,
        Guid eventId,
        string messageType,
        string payloadJson,
        Guid? tenantId)
    {
        if (eventId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InvalidEventId);
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InvalidMessageType);
        }

        var message = new IntegrationOutboxMessage(
            id,
            eventId,
            messageType.Trim(),
            payloadJson ?? "{}",
            tenantId,
            IntegrationOutboxMessageStatus.Pending,
            attemptCount: 0,
            nextAttemptAtUtc: null,
            rowVersion: Array.Empty<byte>());

        return Result<IntegrationOutboxMessage>.Success(message);
    }
}