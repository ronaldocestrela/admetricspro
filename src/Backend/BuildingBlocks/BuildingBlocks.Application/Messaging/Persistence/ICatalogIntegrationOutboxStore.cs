using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Leitura e atualização de outbox no catálogo Master (consumidor de e-mail e reagendamento pós-publicação).
/// </summary>
public interface ICatalogIntegrationOutboxStore
{
    /// <summary>
    /// Obtém a linha de outbox pelo <see cref="IntegrationOutboxMessage.EventId"/>.
    /// </summary>
    /// <param name="eventId">Identificador do evento.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linha ou nulo.</returns>
    Task<IntegrationOutboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste alterações na linha (reagendamento ou dead-letter).
    /// </summary>
    /// <param name="message">Linha rastreada.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> UpdateAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista linhas em dead-letter no catálogo.
    /// </summary>
    /// <param name="limit">Quantidade máxima.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linhas mortas.</returns>
    Task<IReadOnlyList<IntegrationOutboxMessage>> ListDeadLettersAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica reprocessamento pendente por correlação.
    /// </summary>
    /// <param name="originalEventId">Evento original.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns><see langword="true"/> quando há fila pendente.</returns>
    Task<bool> ExistsPendingReprocessForOriginalEventAsync(Guid originalEventId, CancellationToken cancellationToken = default);
}