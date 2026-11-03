using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Publications;

/// <summary>
/// Agregado outbox que representa uma tentativa de publicação orgânica para um slot agendado.
/// </summary>
public sealed class PublicationJob : AggregateRoot<Guid>
{
    private PublicationJob(
        Guid id,
        Guid postId,
        Guid workspaceId,
        DateTime scheduledAtUtc,
        string idempotencyKey,
        PublicationJobStatus status,
        int attemptCount,
        DateTime? nextAttemptAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        PostId = postId;
        WorkspaceId = workspaceId;
        ScheduledAtUtc = scheduledAtUtc;
        IdempotencyKey = idempotencyKey;
        Status = status;
        AttemptCount = attemptCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    private PublicationJob()
        : base(Guid.Empty)
    {
        IdempotencyKey = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do post orgânico associado.
    /// </summary>
    public Guid PostId { get; private set; }

    /// <summary>
    /// Obtém o workspace proprietário.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o horário UTC do slot no momento da abertura do job.
    /// </summary>
    public DateTime ScheduledAtUtc { get; private set; }

    /// <summary>
    /// Obtém a chave de idempotência única do slot.
    /// </summary>
    public string IdempotencyKey { get; private set; }

    /// <summary>
    /// Obtém o status atual do job.
    /// </summary>
    public PublicationJobStatus Status { get; private set; }

    /// <summary>
    /// Obtém a quantidade de tentativas de publicação já realizadas.
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Obtém o próximo instante UTC de retentativa, quando aplicável.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; private set; }

    /// <summary>
    /// Obtém o carimbo UTC de criação do job.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o token de concorrência otimista para claim exclusivo.
    /// </summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// Indica se o job pode ser reivindicado por um worker.
    /// </summary>
    public bool CanBeClaimed =>
        Status is PublicationJobStatus.Pending or PublicationJobStatus.RetryScheduled;

    /// <summary>
    /// Abre um job pendente para o slot informado.
    /// </summary>
    /// <param name="postId">Post orgânico.</param>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="scheduledAtUtc">Horário UTC do slot.</param>
    /// <param name="createdAtUtc">Instante de criação em UTC.</param>
    /// <returns>Job em <see cref="PublicationJobStatus.Pending"/> ou erro de validação.</returns>
    public static Result<PublicationJob> Open(
        Guid postId,
        Guid workspaceId,
        DateTime scheduledAtUtc,
        DateTime createdAtUtc)
    {
        if (postId == Guid.Empty)
        {
            return Result<PublicationJob>.Failure(SocialErrors.PostNotFound);
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<PublicationJob>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (scheduledAtUtc.Kind != DateTimeKind.Utc || createdAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<PublicationJob>.Failure(SocialErrors.ScheduleInstantNotUtc);
        }

        var key = PublicationJobKey.Build(postId, scheduledAtUtc);
        return Result<PublicationJob>.Success(
            new PublicationJob(
                Guid.NewGuid(),
                postId,
                workspaceId,
                scheduledAtUtc,
                key,
                PublicationJobStatus.Pending,
                attemptCount: 0,
                nextAttemptAtUtc: null,
                createdAtUtc));
    }

    /// <summary>
    /// Cancela o job quando ainda não foi concluído nem está em processamento definitivo.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result Cancel()
    {
        if (Status is PublicationJobStatus.Succeeded or PublicationJobStatus.DeadLetter or PublicationJobStatus.Cancelled)
        {
            return Result.Failure(SocialErrors.PublicationJobNotCancellable);
        }

        if (Status == PublicationJobStatus.InProgress)
        {
            return Result.Failure(SocialErrors.PublicationJobInProgress);
        }

        Status = PublicationJobStatus.Cancelled;
        NextAttemptAtUtc = null;
        return Result.Success();
    }

    /// <summary>
    /// Marca o job como reivindicado para processamento exclusivo.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result MarkInProgress()
    {
        if (!CanBeClaimed)
        {
            return Result.Failure(SocialErrors.PublicationJobNotClaimable);
        }

        Status = PublicationJobStatus.InProgress;
        return Result.Success();
    }

    /// <summary>
    /// Marca publicação bem-sucedida.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result MarkSucceeded()
    {
        if (Status != PublicationJobStatus.InProgress)
        {
            return Result.Failure(SocialErrors.PublicationJobInvalidTransition);
        }

        Status = PublicationJobStatus.Succeeded;
        NextAttemptAtUtc = null;
        return Result.Success();
    }

    /// <summary>
    /// Agenda retentativa após rate limit ou falha recuperável.
    /// </summary>
    /// <param name="nextAttemptAtUtc">Próximo instante UTC de tentativa.</param>
    /// <param name="attemptCount">Contagem atualizada de tentativas.</param>
    /// <returns>Resultado da transição.</returns>
    public Result ScheduleRetry(DateTime nextAttemptAtUtc, int attemptCount)
    {
        if (Status != PublicationJobStatus.InProgress)
        {
            return Result.Failure(SocialErrors.PublicationJobInvalidTransition);
        }

        if (nextAttemptAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(SocialErrors.ScheduleInstantNotUtc);
        }

        Status = PublicationJobStatus.RetryScheduled;
        NextAttemptAtUtc = nextAttemptAtUtc;
        AttemptCount = attemptCount;
        return Result.Success();
    }

    /// <summary>
    /// Marca falha definitiva após esgotar retentativas.
    /// </summary>
    /// <returns>Resultado da transição.</returns>
    public Result MarkDeadLetter()
    {
        if (Status != PublicationJobStatus.InProgress)
        {
            return Result.Failure(SocialErrors.PublicationJobInvalidTransition);
        }

        Status = PublicationJobStatus.DeadLetter;
        NextAttemptAtUtc = null;
        return Result.Success();
    }
}
