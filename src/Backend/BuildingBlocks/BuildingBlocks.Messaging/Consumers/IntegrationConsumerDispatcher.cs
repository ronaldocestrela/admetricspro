using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Contracts;
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
        var dispatch = await DispatchWithDispositionAsync(transport, commandFactory, cancellationToken);
        if (dispatch.Disposition.Action == IntegrationMessageDispositionAction.Acknowledge)
        {
            return Result.Success();
        }

        if (dispatch.Disposition.Action == IntegrationMessageDispositionAction.Reschedule)
        {
            return Result.Failure(MessagingErrors.PublishFailed);
        }

        return dispatch.Result.IsFailure ? dispatch.Result : Result.Failure(MessagingErrors.PublishFailed);
    }

    /// <summary>
    /// Processa a mensagem e expõe a <see cref="IntegrationResultDisposition"/> para consumidores que reagendam outbox.
    /// </summary>
    /// <param name="transport">Mensagem recebida do broker.</param>
    /// <param name="commandFactory">Fábrica do comando MediatR a partir do envelope.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado com disposição explícita.</returns>
    public async Task<IntegrationConsumerDispatchResult> DispatchWithDispositionAsync(
        IntegrationEventTransportMessage transport,
        Func<IntegrationEventEnvelope, IRequest<Result>> commandFactory,
        CancellationToken cancellationToken)
    {
        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(transport.Json);
        if (envelopeResult.IsFailure)
        {
            var invalidDisposition = IntegrationResultDisposition.ForResult(envelopeResult, alreadyInInbox: false);
            return IntegrationConsumerDispatchResult.Create(envelopeResult, invalidDisposition);
        }

        var envelope = envelopeResult.Value;

        if (envelope.Scope == IntegrationEventScope.Operational)
        {
            if (!envelope.TenantId.HasValue)
            {
                var tenantError = Result.Failure(MessagingErrors.TenantRequired);
                return IntegrationConsumerDispatchResult.Create(
                    tenantError,
                    IntegrationResultDisposition.ForResult(tenantError, alreadyInInbox: false));
            }

            tenantContextAccessor.TenantContext = TenantContext.Create(
                envelope.TenantId,
                subdomain: null,
                TenantResolutionSource.None);
        }

        var alreadyInInbox = await inboxStore.ExistsAsync(envelope.EventId, consumerName, cancellationToken);
        var skipDisposition = IntegrationResultDisposition.ForResult(Result.Success(), alreadyInInbox);
        if (skipDisposition.Action == IntegrationMessageDispositionAction.Acknowledge && alreadyInInbox)
        {
            return IntegrationConsumerDispatchResult.Create(Result.Success(), skipDisposition);
        }

        var command = commandFactory(envelope);
        var commandResult = await sender.Send(command, cancellationToken);
        var disposition = IntegrationResultDisposition.ForResult(commandResult, alreadyInInbox: false);

        if (disposition.Action == IntegrationMessageDispositionAction.Acknowledge)
        {
            await inboxStore.TryRecordAsync(envelope.EventId, consumerName, DateTime.UtcNow, cancellationToken);
        }

        return IntegrationConsumerDispatchResult.Create(commandResult, disposition);
    }
}