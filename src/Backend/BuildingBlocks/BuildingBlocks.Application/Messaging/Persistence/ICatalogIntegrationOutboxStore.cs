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
}