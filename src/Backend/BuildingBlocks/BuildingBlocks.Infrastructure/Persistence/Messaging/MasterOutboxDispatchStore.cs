using BuildingBlocks.Domain.Messaging;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Facade de outbox no MasterDb para o dispatcher (evita colisão de <c>IIntegrationOutboxStore</c> com o tenant).
/// </summary>
public sealed class MasterOutboxDispatchStore(DbIntegrationOutboxStore inner)
{
    /// <summary>
    /// Reivindica a próxima linha elegível no catálogo.
    /// </summary>
    /// <param name="utcNow">Relógio UTC.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linha claimada ou nulo.</returns>
    public Task<IntegrationOutboxMessage?> TryClaimNextAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        inner.TryClaimNextAsync(utcNow, cancellationToken);

    /// <summary>
    /// Marca linha como publicada.
    /// </summary>
    /// <param name="message">Linha em processamento.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Tarefa de persistência.</returns>
    public Task MarkPublishedAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default) =>
        inner.MarkPublishedAsync(message, cancellationToken);

    /// <summary>
    /// Reagenda linha após falha de publicação.
    /// </summary>
    /// <param name="message">Linha em processamento.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Tarefa de persistência.</returns>
    public Task RequeueAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default) =>
        inner.RequeueAsync(message, cancellationToken);

    /// <summary>
    /// Reclaim de linhas órfãs em processamento.
    /// </summary>
    /// <param name="utcNow">Relógio UTC.</param>
    /// <param name="inProgressTimeout">Timeout de processamento.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Quantidade reclaimada.</returns>
    public Task<int> ReclaimStaleAsync(DateTime utcNow, TimeSpan inProgressTimeout, CancellationToken cancellationToken = default) =>
        inner.ReclaimStaleAsync(utcNow, inProgressTimeout, cancellationToken);
}