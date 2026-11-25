using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Grava linhas de outbox no <c>MasterDb</c> na mesma transação de comandos de catálogo.
/// Distinto de <see cref="IIntegrationOutboxWriter"/>, que no host Web API resolve o contexto do tenant.
/// </summary>
public interface ICatalogIntegrationOutboxWriter
{
    /// <summary>
    /// Adiciona uma linha de outbox ao contexto Master; o commit fica a cargo do <c>UnitOfWork</c>.
    /// </summary>
    /// <param name="message">Linha criada pelo domínio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> AddAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);
}