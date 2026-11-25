using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Lease exclusivo do dispatcher no MasterDb.
/// </summary>
public interface IMessagingSchedulerLeaseStore
{
    /// <summary>
    /// Tenta adquirir ou renovar o lease de despacho de outbox.
    /// </summary>
    /// <param name="leaseKey">Chave do lease.</param>
    /// <param name="holderInstanceId">Identificador da instância atual.</param>
    /// <param name="utcNow">Relógio de referência.</param>
    /// <param name="leaseDuration">Duração do lease.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso quando a instância detém o lease.</returns>
    Task<Result> TryAcquireOrRenewAsync(
        string leaseKey,
        string holderInstanceId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);
}