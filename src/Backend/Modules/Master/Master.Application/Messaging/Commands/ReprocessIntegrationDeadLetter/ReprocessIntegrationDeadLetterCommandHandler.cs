using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Master.Application.Messaging.Commands.ReprocessIntegrationDeadLetter;

/// <summary>
/// Cria nova linha Pending a partir de dead-letter sem editar JSON manualmente.
/// </summary>
public sealed class ReprocessIntegrationDeadLetterCommandHandler(
    ICatalogIntegrationOutboxStore catalogStore,
    ICatalogIntegrationOutboxWriter catalogWriter,
    ITenantIntegrationOutboxAdministration tenantAdministration) : IRequestHandler<ReprocessIntegrationDeadLetterCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        ReprocessIntegrationDeadLetterCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId is null)
        {
            return await ReprocessCatalogAsync(request.EventId, cancellationToken);
        }

        return await tenantAdministration.ReprocessDeadLetterAsync(
            request.TenantId.Value,
            request.EventId,
            cancellationToken);
    }

    private async Task<Result<Guid>> ReprocessCatalogAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var line = await catalogStore.GetByEventIdAsync(eventId, cancellationToken);
        if (line is null)
        {
            return Result<Guid>.Failure(Error.NotFound("Messaging.DeadLetterNotFound", "Dead-letter não localizada."));
        }

        if (await catalogStore.ExistsPendingReprocessForOriginalEventAsync(eventId, cancellationToken))
        {
            return Result<Guid>.Failure(MessagingErrors.ReprocessPending);
        }

        var create = IntegrationDeadLetterReprocessFactory.CreateReprocessLine(line, DateTime.UtcNow);
        if (create.IsFailure)
        {
            return Result<Guid>.Failure(create.Error);
        }

        var add = await catalogWriter.AddAsync(create.Value, cancellationToken);
        return add.IsSuccess
            ? Result<Guid>.Success(create.Value.EventId)
            : Result<Guid>.Failure(add.Error);
    }
}