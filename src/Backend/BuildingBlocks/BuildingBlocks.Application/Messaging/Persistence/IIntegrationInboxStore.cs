using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Idempotência de transporte por consumidor.
/// </summary>
public interface IIntegrationInboxStore
{
    /// <summary>
    /// Verifica se o par evento/consumidor já foi processado.
    /// </summary>
    /// <param name="eventId">Identificador do evento.</param>
    /// <param name="consumerName">Nome do consumidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="true"/> quando já existe registro.</returns>
    Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grava o registro de inbox na transação atual.
    /// </summary>
    /// <param name="eventId">Identificador do evento.</param>
    /// <param name="consumerName">Nome do consumidor.</param>
    /// <param name="processedAtUtc">Instante de processamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso ou conflito quando já processado.</returns>
    Task<Result> TryRecordAsync(
        Guid eventId,
        string consumerName,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default);
}