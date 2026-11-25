using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Transport;

/// <summary>
/// Publica uma linha de outbox já claimada no broker de integração.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publica o payload da linha no exchange de integração.
    /// </summary>
    /// <param name="message">Linha em <see cref="IntegrationOutboxMessageStatus.InProgress"/>.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso após confirmação do broker.</returns>
    Task<Result> PublishAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default);
}