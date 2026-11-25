using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Operações administrativas de outbox em banco de tenant sem expor resolução de contexto ao módulo Master.
/// </summary>
public interface ITenantIntegrationOutboxAdministration
{
    /// <summary>
    /// Lista dead-letters do tenant informado.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="subdomain">Subdomínio para contexto.</param>
    /// <param name="limit">Limite de linhas.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Itens mapeados.</returns>
    Task<IReadOnlyList<IntegrationDeadLetterListItemDto>> ListDeadLettersAsync(
        Guid tenantId,
        string subdomain,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocessa dead-letter operacional do tenant.
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="eventId">Evento morto.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Novo EventId da linha Pending.</returns>
    Task<Result<Guid>> ReprocessDeadLetterAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken = default);
}