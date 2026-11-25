using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

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

    /// <summary>
    /// Obtém a linha de outbox pelo <see cref="IntegrationOutboxMessage.EventId"/>.
    /// </summary>
    /// <param name="eventId">Identificador do evento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Linha encontrada ou <see langword="null"/>.</returns>
    Task<IntegrationOutboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista linhas em dead-letter para operação, mais recentes primeiro.
    /// </summary>
    /// <param name="limit">Quantidade máxima.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linhas mortas.</returns>
    Task<IReadOnlyList<IntegrationOutboxMessage>> ListDeadLettersAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se há reprocessamento pendente para o evento original (correlation).
    /// </summary>
    /// <param name="originalEventId">EventId da linha morta.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns><see langword="true"/> quando existe linha Pending/InProgress com mesma correlação.</returns>
    Task<bool> ExistsPendingReprocessForOriginalEventAsync(Guid originalEventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste alterações em uma linha já rastreada ou atualizada.
    /// </summary>
    /// <param name="message">Linha de outbox.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> UpdateAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);
}