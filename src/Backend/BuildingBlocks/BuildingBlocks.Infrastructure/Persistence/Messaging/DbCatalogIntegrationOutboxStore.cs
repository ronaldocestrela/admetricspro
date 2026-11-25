using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Store EF de outbox do catálogo Master para reagendamento pós-consumo.
/// </summary>
public sealed class DbCatalogIntegrationOutboxStore(DbContext context) : ICatalogIntegrationOutboxStore
{
    /// <inheritdoc />
    public async Task<IntegrationOutboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await context.Set<IntegrationOutboxMessage>()
            .FirstOrDefaultAsync(m => m.EventId == eventId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Set<IntegrationOutboxMessage>().Update(message);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}