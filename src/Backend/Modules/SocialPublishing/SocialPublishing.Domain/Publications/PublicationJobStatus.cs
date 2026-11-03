namespace SocialPublishing.Domain.Publications;

/// <summary>
/// Status do job de publicação orgânica na outbox do tenant.
/// </summary>
public enum PublicationJobStatus
{
    /// <summary>
    /// Aguardando o horário ou claim do worker.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Reivindicado por um worker e em processamento.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Falha recuperável; aguarda <see cref="PublicationJob.NextAttemptAtUtc"/>.
    /// </summary>
    RetryScheduled = 2,

    /// <summary>
    /// Publicação concluída com sucesso.
    /// </summary>
    Succeeded = 3,

    /// <summary>
    /// Esgotou tentativas ou falha definitiva.
    /// </summary>
    DeadLetter = 4,

    /// <summary>
    /// Cancelado por reagendamento ou revogação do slot.
    /// </summary>
    Cancelled = 5,
}
