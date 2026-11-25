using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Executa operações administrativas de outbox em escopo de tenant.
/// </summary>
/// <param name="scopeFactory">Fábrica de escopos.</param>
public sealed class TenantIntegrationOutboxAdministration(IServiceScopeFactory scopeFactory)
    : ITenantIntegrationOutboxAdministration
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<IntegrationDeadLetterListItemDto>> ListDeadLettersAsync(
        Guid tenantId,
        string subdomain,
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext =
            TenantContext.Create(tenantId, subdomain, TenantResolutionSource.None);

        var store = scope.ServiceProvider.GetRequiredService<IIntegrationOutboxStore>();
        var lines = await store.ListDeadLettersAsync(limit, cancellationToken);
        return lines.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> ReprocessDeadLetterAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext =
            TenantContext.Create(tenantId, null, TenantResolutionSource.None);

        var store = scope.ServiceProvider.GetRequiredService<IIntegrationOutboxStore>();
        var writer = scope.ServiceProvider.GetRequiredService<IIntegrationOutboxWriter>();
        var line = await store.GetByEventIdAsync(eventId, cancellationToken);
        if (line is null)
        {
            return Result<Guid>.Failure(Error.NotFound("Messaging.DeadLetterNotFound", "Dead-letter não localizada."));
        }

        if (await store.ExistsPendingReprocessForOriginalEventAsync(eventId, cancellationToken))
        {
            return Result<Guid>.Failure(MessagingErrors.ReprocessPending);
        }

        var create = IntegrationDeadLetterReprocessFactory.CreateReprocessLine(line, DateTime.UtcNow);
        if (create.IsFailure)
        {
            return Result<Guid>.Failure(create.Error);
        }

        var add = await writer.AddAsync(create.Value, cancellationToken);
        return add.IsSuccess
            ? Result<Guid>.Success(create.Value.EventId)
            : Result<Guid>.Failure(add.Error);
    }

    private static IntegrationDeadLetterListItemDto Map(IntegrationOutboxMessage line) =>
        new(
            line.EventId,
            line.MessageType,
            line.TenantId,
            line.AttemptCount,
            line.LastErrorCode,
            line.CreatedAtUtc);
}