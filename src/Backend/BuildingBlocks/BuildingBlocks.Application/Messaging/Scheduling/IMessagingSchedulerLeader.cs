using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Scheduling;

/// <summary>
/// Porta de liderança exclusiva para laços temporais de mensageria (Fase 10).
/// </summary>
public interface IMessagingSchedulerLeader
{
    /// <summary>
    /// Tenta adquirir ou renovar o lease compartilhado de scheduler.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso quando esta instância detém o lease.</returns>
    Task<Result> TryLeadCycleAsync(CancellationToken cancellationToken = default);
}