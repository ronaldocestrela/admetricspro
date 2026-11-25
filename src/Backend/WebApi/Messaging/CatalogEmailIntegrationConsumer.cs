using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using BuildingBlocks.Messaging.Consumers;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using Master.Application.Emails.Commands.SendSubscriptionConfirmationEmail;
using Master.Application.Emails.Commands.SendTenantWelcomeEmail;
using MediatR;
using Microsoft.Extensions.Options;
using WebApi.Configuration;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor MassTransit de e-mails transacionais do catálogo Master.
/// </summary>
public sealed class CatalogEmailIntegrationConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogEmailConsumerOptions> consumerOptions) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome estável do consumidor para inbox no MasterDb.
    /// </summary>
    public const string ConsumerName = "admetricspro.master.catalog-email";

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<IntegrationEventTransportMessage> context)
    {
        var messageType = context.Message.MessageType;
        if (messageType is not CatalogIntegrationMessageTypes.TenantWelcomeEmail
            and not CatalogIntegrationMessageTypes.SubscriptionConfirmationEmail)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var masterInbox = services.GetRequiredService<MasterIntegrationInboxStore>();
        var dispatcher = new IntegrationConsumerDispatcher(
            services.GetRequiredService<ISender>(),
            masterInbox,
            services.GetRequiredService<BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor>(),
            ConsumerName);

        Func<IntegrationEventEnvelope, IRequest<Result>> factory = messageType switch
        {
            CatalogIntegrationMessageTypes.TenantWelcomeEmail => env =>
                new SendTenantWelcomeEmailCommand(
                    env.EventId,
                    env.Payload.GetProperty("tenantId").GetGuid()),
            CatalogIntegrationMessageTypes.SubscriptionConfirmationEmail => env =>
                new SendSubscriptionConfirmationEmailCommand(
                    env.EventId,
                    env.Payload.GetProperty("tenantId").GetGuid()),
            _ => _ => throw new InvalidOperationException("Tipo de mensagem não suportado.")
        };

        var dispatch = await dispatcher.DispatchWithDispositionAsync(
            context.Message,
            factory,
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
                    : "Falha inesperada no consumidor de e-mail de catálogo.");
        }

        if (dispatch.Disposition.Action == IntegrationMessageDispositionAction.Reschedule)
        {
            await ApplyConsumerRescheduleAsync(
                services,
                context.Message,
                dispatch,
                context.CancellationToken);
        }
    }

    private async Task ApplyConsumerRescheduleAsync(
        IServiceProvider services,
        IntegrationEventTransportMessage transport,
        IntegrationConsumerDispatchResult dispatch,
        CancellationToken cancellationToken)
    {
        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(transport.Json, IntegrationEventScope.Catalog);
        if (envelopeResult.IsFailure)
        {
            return;
        }

        var eventId = envelopeResult.Value.EventId;
        var outboxStore = services.GetRequiredService<ICatalogIntegrationOutboxStore>();
        var line = await outboxStore.GetByEventIdAsync(eventId, cancellationToken);
        if (line is null)
        {
            return;
        }

        var maxAttempts = Math.Max(1, consumerOptions.Value.MaxAttempts);
        if (line.AttemptCount >= maxAttempts)
        {
            line.MarkDeadLetter(dispatch.Result.Error.Code);
            await outboxStore.UpdateAsync(line, cancellationToken);
            return;
        }

        var nextAttempt = dispatch.Disposition.NextAttemptAtUtc?.UtcDateTime ?? DateTime.UtcNow.AddMinutes(1);
        line.ScheduleForConsumerRetry(nextAttempt, dispatch.Result.Error.Code, DateTime.UtcNow);
        await outboxStore.UpdateAsync(line, cancellationToken);
    }
}