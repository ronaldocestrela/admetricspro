namespace SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;

/// <summary>
/// Resumo da execução de disparo de publicações orgânicas em um tenant.
/// </summary>
/// <param name="SucceededCount">Jobs concluídos com sucesso.</param>
/// <param name="RetryScheduledCount">Jobs reagendados para retentativa.</param>
/// <param name="DeadLetterCount">Jobs marcados como falha definitiva.</param>
/// <param name="SkippedCount">Jobs ignorados (claim perdido ou post inelegível).</param>
public sealed record DispatchDuePublicationsSummary(
    int SucceededCount,
    int RetryScheduledCount,
    int DeadLetterCount,
    int SkippedCount);
