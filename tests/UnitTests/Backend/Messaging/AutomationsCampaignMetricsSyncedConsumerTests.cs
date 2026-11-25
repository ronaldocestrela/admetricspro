using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Contracts;
using Automations.Application.Ingestion.Commands.ReactToCampaignMetricsSynced;
using FluentAssertions;
using Integrations.Application.Messaging;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using WebApi.Configuration;
using WebApi.Messaging;

namespace UnitTests.Backend.Messaging;

/// <summary>Consumidor de automações após métricas sincronizadas.</summary>
public sealed class AutomationsCampaignMetricsSyncedConsumerTests
{
    /// <summary>Reentrega com inbox existente não chama MediatR.</summary>
    [Fact]
    public async Task Consume_WhenAlreadyInInbox_DoesNotDispatchCommand()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var transport = CreateTransport(tenantId, workspaceId, accountId, eventId);

        var inbox = Substitute.For<IIntegrationInboxStore>();
        inbox.ExistsAsync(Arg.Any<Guid>(), AutomationsCampaignMetricsSyncedConsumer.ConsumerName, Arg.Any<CancellationToken>()).Returns(true);

        var sender = Substitute.For<ISender>();
        var consumer = BuildConsumer(inbox, sender);
        await consumer.Consume(CreateContext(transport));

        await sender.DidNotReceive().Send(Arg.Any<ReactToCampaignMetricsSyncedCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Primeira entrega processa e grava inbox.</summary>
    [Fact]
    public async Task Consume_WhenNewMessage_DispatchesAndRecordsInbox()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var transport = CreateTransport(tenantId, workspaceId, accountId, eventId);

        var inbox = Substitute.For<IIntegrationInboxStore>();
        inbox.ExistsAsync(eventId, AutomationsCampaignMetricsSyncedConsumer.ConsumerName, Arg.Any<CancellationToken>()).Returns(false);
        inbox.TryRecordAsync(eventId, AutomationsCampaignMetricsSyncedConsumer.ConsumerName, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReactToCampaignMetricsSyncedCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var consumer = BuildConsumer(inbox, sender);
        await consumer.Consume(CreateContext(transport));

        await sender.Received(1).Send(Arg.Any<ReactToCampaignMetricsSyncedCommand>(), Arg.Any<CancellationToken>());
        await inbox.Received(1).TryRecordAsync(Arg.Any<Guid>(), AutomationsCampaignMetricsSyncedConsumer.ConsumerName, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private static AutomationsCampaignMetricsSyncedConsumer BuildConsumer(IIntegrationInboxStore inbox, ISender sender)
    {
        var services = new ServiceCollection();
        services.AddSingleton(inbox);
        services.AddSingleton(sender);
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddSingleton<IOptions<IngestionReactionsConsumerOptions>>(Options.Create(new IngestionReactionsConsumerOptions()));
        var provider = services.BuildServiceProvider();
        return new AutomationsCampaignMetricsSyncedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IngestionReactionsConsumerOptions()));
    }

    private static IntegrationEventTransportMessage CreateTransport(Guid tenantId, Guid workspaceId, Guid accountId, Guid eventId)
    {
        var syncedAt = DateTime.UtcNow;
        var start = syncedAt.Date.AddDays(-7);
        var end = syncedAt.Date;
        var outbox = CampaignIngestionOutboxFactory.CreateMetricsSynced(
            tenantId, workspaceId, accountId, "MetaAds", BuildingBlocks.Domain.Campaigns.MetricGranularity.Daily, start, end, syncedAt).Value;
        return new IntegrationEventTransportMessage
        {
            MessageType = IntegrationsIntegrationMessageTypes.CampaignMetricsSynced,
            Json = outbox.PayloadJson,
            EventId = outbox.EventId,
            TenantId = tenantId,
        };
    }

    private static ConsumeContext<IntegrationEventTransportMessage> CreateContext(IntegrationEventTransportMessage message)
    {
        var context = Substitute.For<ConsumeContext<IntegrationEventTransportMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}