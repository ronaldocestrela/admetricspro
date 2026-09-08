namespace WebApi.Models;

/// <summary>
/// Requisição para execução manual do ciclo de avaliação da régua de trial.
/// </summary>
/// <param name="ReferenceDateUtc">Data e hora UTC de referência opcional para simulação ou conciliação.</param>
public sealed record ExecuteTrialNoticeApiRequest(DateTime? ReferenceDateUtc = null);

/// <summary>
/// Resposta com métricas e sumário de execução do ciclo de notificações de trial.
/// </summary>
/// <param name="EvaluatedCount">Total de tenants em trial avaliados no ciclo.</param>
/// <param name="SevenDayNoticesSent">Total de lembretes de 7 dias restantes enviados.</param>
/// <param name="ThreeDayNoticesSent">Total de lembretes de 3 dias restantes enviados.</param>
/// <param name="OneDayNoticesSent">Total de lembretes de 1 dia restante enviados.</param>
/// <param name="ExpiredNoticesSent">Total de notificações de expiração enviadas.</param>
/// <param name="FailuresCount">Total de falhas no envio de notificações.</param>
/// <param name="TotalNoticesSent">Total de avisos enviados com sucesso.</param>
/// <param name="ExecutedAtUtc">Timestamp UTC de execução do ciclo.</param>
public sealed record TrialNoticeExecutionSummaryResponse(
    int EvaluatedCount,
    int SevenDayNoticesSent,
    int ThreeDayNoticesSent,
    int OneDayNoticesSent,
    int ExpiredNoticesSent,
    int FailuresCount,
    int TotalNoticesSent,
    DateTime ExecutedAtUtc);
