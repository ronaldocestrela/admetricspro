using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Queries.GetTenants;
using Master.Domain.Tenants;
using MediatR;

namespace Master.Application.Messaging.Queries.GetIntegrationDeadLetters;

/// <summary>
/// Agrega dead-letters do Master e dos bancos operacionais elegíveis.
/// </summary>
public sealed class GetIntegrationDeadLettersQueryHandler(
    ISender sender,
    ICatalogIntegrationOutboxStore catalogStore,
    ITenantIntegrationOutboxAdministration tenantAdministration) : IRequestHandler<GetIntegrationDeadLettersQuery, Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>> Handle(
        GetIntegrationDeadLettersQuery request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var items = new List<IntegrationDeadLetterListItemDto>();

        var catalogLines = await catalogStore.ListDeadLettersAsync(limit, cancellationToken);
        items.AddRange(catalogLines.Select(Map));

        var tenants = await sender.Send(new GetTenantsQuery(), cancellationToken);
        if (tenants.IsFailure)
        {
            return Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>.Success(items);
        }

        foreach (var tenant in tenants.Value.Where(t =>
                     t.Status is nameof(TenantStatus.Active) or nameof(TenantStatus.Trial)))
        {
            try
            {
                var lines = await tenantAdministration.ListDeadLettersAsync(
                    tenant.Id,
                    tenant.Subdomain,
                    limit,
                    cancellationToken);
                items.AddRange(lines);
            }
            catch
            {
                // Continua com o próximo tenant sem abortar a consulta administrativa.
            }
        }

        return Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>.Success(
            items.OrderByDescending(i => i.CreatedAtUtc).Take(limit * 2).ToList());
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