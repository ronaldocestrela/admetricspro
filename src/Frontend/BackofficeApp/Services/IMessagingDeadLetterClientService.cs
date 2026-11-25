using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BackofficeApp.Services;

/// <summary>
/// Cliente HTTP para dead-letters de mensageria no Backoffice.
/// </summary>
public interface IMessagingDeadLetterClientService
{
    /// <summary>
    /// Lista dead-letters administrativas.
    /// </summary>
    /// <param name="limit">Quantidade máxima.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linhas mortas.</returns>
    Task<Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>> GetDeadLettersAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocessa uma linha morta.
    /// </summary>
    /// <param name="eventId">Evento original.</param>
    /// <param name="tenantId">Tenant opcional.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Novo EventId.</returns>
    Task<Result<Guid>> ReprocessAsync(
        Guid eventId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}