using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Transport;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;

namespace BuildingBlocks.Messaging.Transport;

/// <summary>
/// Publica linhas de outbox no exchange <c>admetricspro.integration</c> via MassTransit.
/// </summary>
public sealed class IntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    /// <inheritdoc />
    public async Task<Result> PublishAsync(IntegrationOutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(message.PayloadJson);
        if (envelopeResult.IsFailure)
        {
            return Result.Failure(envelopeResult.Error);
        }

        var envelope = envelopeResult.Value;
        var transport = new IntegrationEventTransportMessage
        {
            Json = IntegrationEventEnvelopeSerializer.Serialize(envelope),
            MessageType = message.MessageType,
            EventId = envelope.EventId,
            TenantId = envelope.TenantId,
        };

        try
        {
            await publishEndpoint.Publish(
                transport,
                context =>
                {
                    context.SetRoutingKey(message.MessageType);
                    context.Headers.Set("message-id", envelope.EventId.ToString());
                    context.Headers.Set("correlation-id", envelope.CorrelationId.ToString());
                    context.Headers.Set("message-type", envelope.MessageType);
                    context.Headers.Set("occurred-on-utc", envelope.OccurredOnUtc.ToString("O"));
                    if (envelope.TenantId.HasValue)
                    {
                        context.Headers.Set("tenant-id", envelope.TenantId.Value.ToString());
                    }
                },
                cancellationToken);

            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(MessagingErrors.PublishFailed);
        }
    }
}