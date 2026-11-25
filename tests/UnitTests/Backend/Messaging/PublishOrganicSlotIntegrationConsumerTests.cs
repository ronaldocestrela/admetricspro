using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Contracts;
using FluentAssertions;
using Master.Application.FeatureFlags.Services;
using Master.Domain.FeatureFlags;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Messaging;
using SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;
using SocialPublishing.Application.Publishing.Commands.ReleasePublicationClaim;
using WebApi.Messaging;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes do consumidor de publicação orgânica por slot.
/// </summary>
public sealed class PublishOrganicSlotIntegrationConsumerTests
{
    /// <summary>
    /// Segunda entrega com o mesmo EventId não despacha comando.
    /// </summary>
    [Fact]
    public async Task Consume_EventIdJaNaInbox_NaoDespachaComando()
    {
        var sender = Substitute.For<ISender>();
        var inbox = Substitute.For<IIntegrationInboxStore>();
        var (context, envelopeEventId) = CreateContext();
        inbox.ExistsAsync(envelopeEventId, PublishOrganicSlotIntegrationConsumer.ConsumerName, Arg.Any<CancellationToken>())
            .Returns(true);

        var consumer = CreateConsumer(sender, inbox, killSwitchArmed: false);
        await consumer.Consume(context);

        await sender.DidNotReceive().Send(Arg.Any<DispatchDuePublicationsCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Kill switch armado libera o claim e grava inbox.
    /// </summary>
    [Fact]
    public async Task Consume_KillSwitchArmado_LiberaClaimEAck()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReleasePublicationClaimCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var inbox = Substitute.For<IIntegrationInboxStore>();
        var (context, envelopeEventId) = CreateContext();
        inbox.ExistsAsync(envelopeEventId, PublishOrganicSlotIntegrationConsumer.ConsumerName, Arg.Any<CancellationToken>())
            .Returns(false);

        var consumer = CreateConsumer(sender, inbox, killSwitchArmed: true);
        await consumer.Consume(context);

        await sender.Received(1).Send(Arg.Any<ReleasePublicationClaimCommand>(), Arg.Any<CancellationToken>());
        await inbox.Received(1).TryRecordAsync(envelopeEventId, PublishOrganicSlotIntegrationConsumer.ConsumerName, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<DispatchDuePublicationsCommand>(), Arg.Any<CancellationToken>());
    }

    private static PublishOrganicSlotIntegrationConsumer CreateConsumer(
        ISender sender,
        IIntegrationInboxStore inbox,
        bool killSwitchArmed)
    {
        var featureFlags = Substitute.For<IFeatureFlagService>();
        featureFlags.IsFeatureEnabledAsync(
                Arg.Any<string>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(killSwitchArmed);

        var services = new ServiceCollection();
        services.AddScoped(_ => sender);
        services.AddScoped(_ => inbox);
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped(_ => featureFlags);
        var provider = services.BuildServiceProvider();

        return new PublishOrganicSlotIntegrationConsumer(provider.GetRequiredService<IServiceScopeFactory>(), featureFlags);
    }

    private static (ConsumeContext<IntegrationEventTransportMessage> Context, Guid EnvelopeEventId) CreateContext()
    {
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var claimedAt = DateTime.UtcNow;
        var outbox = PublishOrganicSlotOutboxFactory.Create(tenantId, jobId, "k", claimedAt, claimedAt).Value;
        var envelope = IntegrationEventEnvelopeSerializer.Deserialize(outbox.PayloadJson).Value;

        var transport = new IntegrationEventTransportMessage
        {
            Json = outbox.PayloadJson,
            MessageType = SocialPublishingIntegrationMessageTypes.PublishOrganicSlot,
            EventId = envelope.EventId,
            TenantId = tenantId,
        };

        var context = Substitute.For<ConsumeContext<IntegrationEventTransportMessage>>();
        context.Message.Returns(transport);
        context.CancellationToken.Returns(CancellationToken.None);
        return (context, envelope.EventId);
    }
}