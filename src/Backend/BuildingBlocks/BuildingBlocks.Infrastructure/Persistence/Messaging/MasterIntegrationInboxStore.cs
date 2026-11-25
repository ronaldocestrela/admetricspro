using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Inbox do catálogo Master (evita colisão com <see cref="IIntegrationInboxStore"/> do tenant).
/// </summary>
public sealed class MasterIntegrationInboxStore(DbIntegrationInboxStore inner)
{
    /// <summary>
    /// Verifica existência de registro na inbox do catálogo.
    /// </summary>
    /// <param name="eventId">EventId.</param>
    /// <param name="consumerName">Consumidor.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Verdadeiro se já processado.</returns>
    public Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default) =>
        inner.ExistsAsync(eventId, consumerName, cancellationToken);

    /// <summary>
    /// Tenta gravar registro de inbox no catálogo.
    /// </summary>
    /// <param name="eventId">EventId.</param>
    /// <param name="consumerName">Consumidor.</param>
    /// <param name="processedAtUtc">Instante UTC.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    public Task<Result> TryRecordAsync(
        Guid eventId,
        string consumerName,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default) =>
        inner.TryRecordAsync(eventId, consumerName, processedAtUtc, cancellationToken);
}