using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Grava linhas de outbox no change tracker do contexto transacional atual.
/// </summary>
public interface IIntegrationOutboxWriter
{
    /// <summary>
    /// Adiciona uma linha de outbox ao contexto; o commit fica a cargo do <c>UnitOfWork</c>.
    /// </summary>
    /// <param name="message">Linha criada pelo domínio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> AddAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);
}