using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Consumers;
using BuildingBlocks.Messaging.Contracts;
using Master.Application.FeatureFlags.Services;
using Master.Domain.FeatureFlags;
using MassTransit;
using MediatR;
using SocialPublishing.Application.Messaging;
using SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;
using SocialPublishing.Application.Publishing.Commands.ReleasePublicationClaim;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor que publica um único job orgânico previamente encaminhado pela outbox.
/// </summary>
public sealed class PublishOrganicSlotIntegrationConsumer(
    IServiceScopeFactory scopeFactory,
    IFeatureFlagService featureFlagService) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome estável do consumidor para inbox no banco do tenant.
    /// </summary>
    public const string ConsumerName = "admetricspro.social.publish-organic-slot";

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<IntegrationEventTransportMessage> context)
    {
        if (!string.Equals(
                context.Message.MessageType,
                SocialPublishingIntegrationMessageTypes.PublishOrganicSlot,
                StringComparison.Ordinal))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(context.Message.Json);
        if (envelopeResult.IsFailure)
        {
            throw new IntegrationMessageRejectedException(envelopeResult.Error.Code);
        }

        var envelope = envelopeResult.Value;
        if (!envelope.TenantId.HasValue)
        {
            throw new IntegrationMessageRejectedException(MessagingErrors.TenantRequired.Code);
        }

        services.GetRequiredService<ITenantContextAccessor>().TenantContext = TenantContext.Create(
            envelope.TenantId.Value,
            subdomain: null,
            TenantResolutionSource.None);

        var inbox = services.GetRequiredService<IIntegrationInboxStore>();
        if (await inbox.ExistsAsync(envelope.EventId, ConsumerName, context.CancellationToken))
        {
            return;
        }

        var publicationJobId = envelope.Payload.GetProperty("publicationJobId").GetGuid();
        var claimedAtUtc = envelope.Payload.GetProperty("claimedAtUtc").GetDateTime();
        if (claimedAtUtc.Kind != DateTimeKind.Utc)
        {
            claimedAtUtc = DateTime.SpecifyKind(claimedAtUtc, DateTimeKind.Utc);
        }

        if (await featureFlagService.IsFeatureEnabledAsync(
                MasterCatalogFeatureFlagKeys.SocialPublishingDispatch,
                cancellationToken: context.CancellationToken))
        {
            var sender = services.GetRequiredService<ISender>();
            _ = await sender.Send(
                new ReleasePublicationClaimCommand(publicationJobId, claimedAtUtc),
                context.CancellationToken);
            await inbox.TryRecordAsync(envelope.EventId, ConsumerName, DateTime.UtcNow, context.CancellationToken);
            return;
        }

        var dispatch = await services.GetRequiredService<ISender>().Send(
            new DispatchDuePublicationsCommand(publicationJobId, claimedAtUtc),
            context.CancellationToken);

        var disposition = IntegrationResultDisposition.ForResult(dispatch, alreadyInInbox: false);
        if (disposition.Action == IntegrationMessageDispositionAction.RetryTransport)
        {
            throw new InvalidOperationException("Falha inesperada no consumidor de publicação orgânica.");
        }

        if (disposition.Action == IntegrationMessageDispositionAction.Acknowledge)
        {
            await inbox.TryRecordAsync(envelope.EventId, ConsumerName, DateTime.UtcNow, context.CancellationToken);
        }
    }
}