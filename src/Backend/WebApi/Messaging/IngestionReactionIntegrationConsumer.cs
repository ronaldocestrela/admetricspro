using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Messaging.Consumers;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;
using WebApi.Configuration;

namespace WebApi.Messaging;

/// <summary>
/// Despacha mensagens operacionais de ingestão via <see cref="IntegrationConsumerDispatcher"/> com reschedule na outbox do tenant.
/// </summary>
internal static class IngestionReactionIntegrationConsumer
{
    /// <summary>
    /// Processa uma mensagem de integração de ingestão com inbox e política de ack.
    /// </summary>
    /// <param name="context">Contexto MassTransit.</param>
    /// <param name="scopeFactory">Fábrica de escopo por mensagem.</param>
    /// <param name="consumerName">Nome estável do consumidor (inbox).</param>
    /// <param name="expectedMessageType">Tipo versionado esperado.</param>
    /// <param name="commandFactory">Mapeamento envelope → comando MediatR.</param>
    /// <param name="consumerOptions">Limites de prefetch e tentativas.</param>
    public static async Task ConsumeAsync(
        ConsumeContext<IntegrationEventTransportMessage> context,
        IServiceScopeFactory scopeFactory,
        string consumerName,
        string expectedMessageType,
        Func<IntegrationEventEnvelope, IRequest<Result>> commandFactory,
        IOptions<IngestionReactionsConsumerOptions> consumerOptions)
    {
        if (!string.Equals(context.Message.MessageType, expectedMessageType, StringComparison.Ordinal))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var dispatcher = new IntegrationConsumerDispatcher(
            services.GetRequiredService<ISender>(),
            services.GetRequiredService<IIntegrationInboxStore>(),
            services.GetRequiredService<BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor>(),
            consumerName);

        var dispatch = await dispatcher.DispatchWithDispositionAsync(
            context.Message,
            commandFactory,
            context.CancellationToken);

        if (dispatch.Result.IsFailure
            && dispatch.Result.Error.Code is "Messaging.PayloadInvalid" or "Messaging.TenantRequired")
        {
            throw new IntegrationMessageRejectedException(dispatch.Result.Error.Code);
        }

        if (dispatch.Disposition.Action == IntegrationMessageDispositionAction.RetryTransport)
        {
            throw new InvalidOperationException(
                dispatch.Result.IsFailure
                    ? dispatch.Result.Error.Description
                    : "Falha inesperada no consumidor de reação pós-ingestão.");
        }

        if (dispatch.Disposition.Action == IntegrationMessageDispositionAction.Reschedule)
        {
            await ApplyConsumerRescheduleAsync(services, context.Message, dispatch, consumerOptions.Value, context.CancellationToken);
        }
    }

    private static async Task ApplyConsumerRescheduleAsync(
        IServiceProvider services,
        IntegrationEventTransportMessage transport,
        IntegrationConsumerDispatchResult dispatch,
        IngestionReactionsConsumerOptions options,
        CancellationToken cancellationToken)
    {
        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(transport.Json);
        if (envelopeResult.IsFailure)
        {
            return;
        }

        var eventId = envelopeResult.Value.EventId;
        var outboxStore = services.GetRequiredService<IIntegrationOutboxStore>();
        var line = await outboxStore.GetByEventIdAsync(eventId, cancellationToken);
        if (line is null)
        {
            return;
        }

        var maxAttempts = Math.Max(1, options.MaxAttempts);
        if (line.AttemptCount >= maxAttempts)
        {
            line.MarkDeadLetter(dispatch.Result.Error.Code);
            await outboxStore.UpdateAsync(line, cancellationToken);
            return;
        }

        var nextAttempt = dispatch.Disposition.NextAttemptAtUtc?.UtcDateTime
            ?? DateTime.UtcNow.AddSeconds(Math.Max(1, options.RetryAfterSeconds));
        line.ScheduleForConsumerRetry(nextAttempt, dispatch.Result.Error.Code, DateTime.UtcNow);
        await outboxStore.UpdateAsync(line, cancellationToken);
    }
}