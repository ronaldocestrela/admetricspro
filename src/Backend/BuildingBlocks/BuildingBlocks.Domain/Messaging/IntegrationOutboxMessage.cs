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
        DateTime createdAtUtc,
        DateTime? inProgressSinceUtc,
        string? lastErrorCode,
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
        CreatedAtUtc = createdAtUtc;
        InProgressSinceUtc = inProgressSinceUtc;
        LastErrorCode = lastErrorCode;
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
    /// Obtém o instante UTC em que a linha foi criada.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o instante UTC em que a linha entrou em processamento pelo dispatcher.
    /// </summary>
    public DateTime? InProgressSinceUtc { get; private set; }

    /// <summary>
    /// Obtém o último código de erro registrado na linha.
    /// </summary>
    public string? LastErrorCode { get; private set; }

    /// <summary>
    /// Obtém o token de concorrência otimista para claim exclusivo do dispatcher.
    /// </summary>
    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Indica se a linha pode ser reivindicada pelo dispatcher no instante informado.
    /// </summary>
    /// <param name="utcNow">Relógio de referência em UTC.</param>
    /// <returns><see langword="true"/> quando claim é permitido.</returns>
    public bool CanBeClaimed(DateTime utcNow) =>
        Status == IntegrationOutboxMessageStatus.Pending
        && (NextAttemptAtUtc is null || NextAttemptAtUtc <= utcNow);

    /// <summary>
    /// Cria uma nova linha de outbox em estado <see cref="IntegrationOutboxMessageStatus.Pending"/>.
    /// </summary>
    /// <param name="id">Identificador da linha de outbox.</param>
    /// <param name="eventId">Identificador único do evento.</param>
    /// <param name="messageType">Tipo versionado da mensagem.</param>
    /// <param name="payloadJson">Payload JSON do envelope.</param>
    /// <param name="tenantId">Tenant opcional para eventos operacionais ou de catálogo com contexto.</param>
    /// <param name="createdAtUtc">Instante de criação em UTC; padrão <see cref="DateTime.UtcNow"/>.</param>
    /// <returns>A linha criada ou falha de validação.</returns>
    public static Result<IntegrationOutboxMessage> Create(
        Guid id,
        Guid eventId,
        string messageType,
        string payloadJson,
        Guid? tenantId,
        DateTime? createdAtUtc = null)
    {
        if (eventId == Guid.Empty)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InvalidEventId);
        }

        if (string.IsNullOrWhiteSpace(messageType))
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InvalidMessageType);
        }

        var created = createdAtUtc ?? DateTime.UtcNow;
        if (created.Kind != DateTimeKind.Utc)
        {
            return Result<IntegrationOutboxMessage>.Failure(IntegrationOutboxErrors.InstantNotUtc);
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
            createdAtUtc: created,
            inProgressSinceUtc: null,
            lastErrorCode: null,
            rowVersion: Array.Empty<byte>());

        return Result<IntegrationOutboxMessage>.Success(message);
    }

    /// <summary>
    /// Marca a linha como reivindicada para publicação exclusiva.
    /// </summary>
    /// <param name="utcNow">Instante do claim em UTC.</param>
    /// <returns>Resultado da transição.</returns>
    public Result MarkInProgress(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        if (!CanBeClaimed(utcNow))
        {
            return Result.Failure(IntegrationOutboxErrors.NotClaimable);
        }

        Status = IntegrationOutboxMessageStatus.InProgress;
        InProgressSinceUtc = utcNow;
        return Result.Success();
    }

    /// <summary>
    /// Marca publicação concluída com sucesso no broker.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result MarkPublished()
    {
        if (Status != IntegrationOutboxMessageStatus.InProgress)
        {
            return Result.Failure(IntegrationOutboxErrors.InvalidTransition);
        }

        Status = IntegrationOutboxMessageStatus.Published;
        InProgressSinceUtc = null;
        NextAttemptAtUtc = null;
        LastErrorCode = null;
        return Result.Success();
    }

    /// <summary>
    /// Reagenda a linha após falha transitória de publicação ou consumo.
    /// </summary>
    /// <param name="nextAttemptAtUtc">Próximo instante de tentativa em UTC.</param>
    /// <param name="errorCode">Código de erro registrado na linha.</param>
    /// <param name="utcNow">Relógio de referência em UTC para validação.</param>
    /// <returns>Resultado da transição.</returns>
    public Result Requeue(DateTime nextAttemptAtUtc, string errorCode, DateTime utcNow)
    {
        if (nextAttemptAtUtc.Kind != DateTimeKind.Utc || utcNow.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        if (Status is not IntegrationOutboxMessageStatus.InProgress and not IntegrationOutboxMessageStatus.Pending)
        {
            return Result.Failure(IntegrationOutboxErrors.InvalidTransition);
        }

        Status = IntegrationOutboxMessageStatus.Pending;
        AttemptCount += 1;
        NextAttemptAtUtc = nextAttemptAtUtc;
        LastErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
        InProgressSinceUtc = null;
        return Result.Success();
    }

    /// <summary>
    /// Marca falha permanente da linha.
    /// </summary>
    /// <param name="errorCode">Código de erro registrado.</param>
    /// <returns>Resultado da transição.</returns>
    public Result MarkDeadLetter(string errorCode)
    {
        if (Status is not IntegrationOutboxMessageStatus.Pending and not IntegrationOutboxMessageStatus.InProgress)
        {
            return Result.Failure(IntegrationOutboxErrors.InvalidTransition);
        }

        Status = IntegrationOutboxMessageStatus.DeadLetter;
        InProgressSinceUtc = null;
        LastErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Devolve linha <see cref="IntegrationOutboxMessageStatus.InProgress"/> órfã a <see cref="IntegrationOutboxMessageStatus.Pending"/>.
    /// </summary>
    /// <param name="utcNow">Relógio de referência em UTC.</param>
    /// <param name="inProgressTimeout">Tempo máximo permitido em processamento.</param>
    /// <returns>Resultado da transição.</returns>
    public Result Reclaim(DateTime utcNow, TimeSpan inProgressTimeout)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(IntegrationOutboxErrors.InstantNotUtc);
        }

        if (Status != IntegrationOutboxMessageStatus.InProgress || InProgressSinceUtc is null)
        {
            return Result.Failure(IntegrationOutboxErrors.InvalidTransition);
        }

        if (utcNow - InProgressSinceUtc.Value < inProgressTimeout)
        {
            return Result.Failure(IntegrationOutboxErrors.NotClaimable);
        }

        Status = IntegrationOutboxMessageStatus.Pending;
        InProgressSinceUtc = null;
        return Result.Success();
    }
}