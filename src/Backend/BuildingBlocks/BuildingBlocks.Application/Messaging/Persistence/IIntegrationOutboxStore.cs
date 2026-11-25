using BuildingBlocks.Domain.Messaging;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Operações de leitura, claim e atualização da outbox transacional.
/// </summary>
public interface IIntegrationOutboxStore
{
    /// <summary>
    /// Reivindica a próxima linha elegível para publicação.
    /// </summary>
    /// <param name="utcNow">Relógio de referência em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linha claimada ou <see langword="null"/>.</returns>
    Task<IntegrationOutboxMessage?> TryClaimNextAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca a linha como publicada após confirmação do broker.
    /// </summary>
    /// <param name="message">Linha em processamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de persistência.</returns>
    Task MarkPublishedAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reagenda a linha após falha de publicação.
    /// </summary>
    /// <param name="message">Linha em processamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de persistência.</returns>
    Task RequeueAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devolve linhas <see cref="IntegrationOutboxMessageStatus.InProgress"/> órfãs a <see cref="IntegrationOutboxMessageStatus.Pending"/>.
    /// </summary>
    /// <param name="utcNow">Relógio de referência.</param>
    /// <param name="inProgressTimeout">Tempo máximo em processamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de linhas reclaimadas.</returns>
    Task<int> ReclaimStaleAsync(DateTime utcNow, TimeSpan inProgressTimeout, CancellationToken cancellationToken = default);
}