using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;

namespace BuildingBlocks.Messaging.Consumers;

/// <summary>
/// Despacha mensagens de integração para comandos MediatR com tenant e inbox.
/// </summary>
public sealed class IntegrationConsumerDispatcher(
    ISender sender,
    IIntegrationInboxStore inboxStore,
    ITenantContextAccessor tenantContextAccessor,
    string consumerName)
{
    /// <summary>
    /// Processa a mensagem de transporte aplicando política de ack via <see cref="IntegrationResultDisposition"/>.
    /// </summary>
    /// <param name="transport">Mensagem recebida do broker.</param>
    /// <param name="commandFactory">Fábrica do comando MediatR a partir do envelope.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Sucesso para ack ou falha permanente que ainda deve ack.</returns>
    public async Task<Result> DispatchAsync(
        IntegrationEventTransportMessage transport,
        Func<IntegrationEventEnvelope, IRequest<Result>> commandFactory,
        CancellationToken cancellationToken)
    {
        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(transport.Json);
        if (envelopeResult.IsFailure)
        {
            return Result.Failure(envelopeResult.Error);
        }

        var envelope = envelopeResult.Value;

        if (envelope.Scope == IntegrationEventScope.Operational)
        {
            if (!envelope.TenantId.HasValue)
            {
                return Result.Failure(MessagingErrors.TenantRequired);
            }

            tenantContextAccessor.TenantContext = TenantContext.Create(
                envelope.TenantId,
                subdomain: null,
                TenantResolutionSource.None);
        }

        var alreadyInInbox = await inboxStore.ExistsAsync(envelope.EventId, consumerName, cancellationToken);
        var disposition = IntegrationResultDisposition.ForResult(Result.Success(), alreadyInInbox);
        if (disposition.Action == IntegrationMessageDispositionAction.Acknowledge && alreadyInInbox)
        {
            return Result.Success();
        }

        var command = commandFactory(envelope);
        var commandResult = await sender.Send(command, cancellationToken);
        disposition = IntegrationResultDisposition.ForResult(commandResult, alreadyInInbox: false);

        if (disposition.Action == IntegrationMessageDispositionAction.Acknowledge)
        {
            await inboxStore.TryRecordAsync(envelope.EventId, consumerName, DateTime.UtcNow, cancellationToken);
            return Result.Success();
        }

        if (disposition.Action == IntegrationMessageDispositionAction.Reschedule)
        {
            return Result.Failure(MessagingErrors.PublishFailed);
        }

        return Result.Failure(MessagingErrors.PublishFailed);
    }
}