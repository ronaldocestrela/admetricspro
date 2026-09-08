namespace Master.Application.Billing.Trial;

/// <summary>
/// Sumário estruturado com as métricas da execução do ciclo de notificações de término de trial.
/// </summary>
/// <param name="EvaluatedCount">Total de tenants em período de testes avaliados.</param>
/// <param name="SevenDayNoticesSent">Quantidade de notificações de 7 dias restantes enviadas.</param>
/// <param name="ThreeDayNoticesSent">Quantidade de notificações de 3 dias restantes enviadas.</param>
/// <param name="OneDayNoticesSent">Quantidade de notificações de 1 dia restante enviadas.</param>
/// <param name="ExpiredNoticesSent">Quantidade de notificações de trial expirado enviadas.</param>
/// <param name="FailuresCount">Quantidade de falhas no envio de mensagens.</param>
/// <param name="ExecutedAtUtc">Data e hora da execução do ciclo em UTC.</param>
public sealed record TrialNoticeExecutionSummary(
    int EvaluatedCount,
    int SevenDayNoticesSent,
    int ThreeDayNoticesSent,
    int OneDayNoticesSent,
    int ExpiredNoticesSent,
    int FailuresCount,
    DateTime ExecutedAtUtc)
{
    /// <summary>
    /// Total geral de avisos disparados com sucesso neste ciclo.
    /// </summary>
    public int TotalNoticesSent => SevenDayNoticesSent + ThreeDayNoticesSent + OneDayNoticesSent + ExpiredNoticesSent;
}
