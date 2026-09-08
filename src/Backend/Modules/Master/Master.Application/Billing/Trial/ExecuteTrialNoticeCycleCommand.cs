using BuildingBlocks.Application.Messaging;

namespace Master.Application.Billing.Trial;

/// <summary>
/// Comando para execução sob demanda do ciclo de avaliações e envio de notificações da régua de trial.
/// </summary>
/// <param name="ReferenceDateUtc">Data e hora de referência UTC opcional para simulação ou conciliação.</param>
public sealed record ExecuteTrialNoticeCycleCommand(DateTime? ReferenceDateUtc = null) : ICommand<TrialNoticeExecutionSummary>;
