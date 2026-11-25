using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Contracts;
using FluentAssertions;
using Integrations.Application.Messaging;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tenants.Application.Integrations.DTOs;
using Tenants.Application.Integrations.Queries.GetConnectedAdAccounts;
using WebApi.Background;
using WebApi.Configuration;
using WebApi.Messaging;

namespace UnitTests.Backend.Messaging;

/// <summary>Consumidor de sync de mídia aplica tenant, inbox e política de ack.</summary>
public sealed class MediaSyncIntegrationConsumerTests
{
  /// <summary>Reentrega não reexecuta o executor.</summary>
  [Fact]
  public async Task Consume_WhenAlreadyInInbox_DoesNotExecuteSync()
  {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var transport = CreateTransport(tenantId, workspaceId, accountId, eventId);

        var inbox = Substitute.For<IIntegrationInboxStore>();
        inbox.ExistsAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<CancellationToken>()).Returns(true);

        var executor = Substitute.For<IConnectedAdAccountMetaSyncExecutor>();

        var consumer = BuildConsumer(inbox, executor, Substitute.For<ISender>(), Substitute.For<IIntegrationOutboxStore>());
        await consumer.Consume(CreateContext(transport));

        await executor.DidNotReceive().ExecuteAsync(Arg.Any<Guid>(), Arg.Any<ConnectedAdAccountDto>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Hierarquia e métricas rodam em sequência com tenant restaurado.</summary>
    [Fact]
    public async Task Consume_WhenNewMessage_ExecutesExecutorAndRecordsInbox()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var transport = CreateTransport(tenantId, workspaceId, accountId, eventId);
        var account = new ConnectedAdAccountDto(accountId, workspaceId, "MetaAds", "act_1", "A1", "BRL", "Connected", false, DateTime.UtcNow);

        var inbox = Substitute.For<IIntegrationInboxStore>();
        inbox.ExistsAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<CancellationToken>()).Returns(false);
        inbox.TryRecordAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetConnectedAdAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ConnectedAdAccountDto>>.Success([account]));

        var executor = Substitute.For<IConnectedAdAccountMetaSyncExecutor>();
        executor.ExecuteAsync(tenantId, account, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var consumer = BuildConsumer(inbox, executor, sender, Substitute.For<IIntegrationOutboxStore>());
        await consumer.Consume(CreateContext(transport));

        await executor.Received(1).ExecuteAsync(tenantId, account, Arg.Any<CancellationToken>());
        await inbox.Received(1).TryRecordAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Rate limit reagenda outbox sem gravar inbox.</summary>
    [Fact]
    public async Task Consume_WhenRateLimited_ReschedulesOutboxWithoutInbox()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var transport = CreateTransport(tenantId, workspaceId, accountId, eventId);
        var account = new ConnectedAdAccountDto(accountId, workspaceId, "MetaAds", "act_1", "A1", "BRL", "Connected", false, DateTime.UtcNow);

        var inbox = Substitute.For<IIntegrationInboxStore>();
        inbox.ExistsAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<CancellationToken>()).Returns(false);

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetConnectedAdAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ConnectedAdAccountDto>>.Success([account]));

        var executor = Substitute.For<IConnectedAdAccountMetaSyncExecutor>();
        executor.ExecuteAsync(tenantId, account, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("Meta.RateLimited", "Limitado")));

        var published = IntegrationOutboxMessage.Create(
                Guid.NewGuid(),
                eventId,
                IntegrationsIntegrationMessageTypes.AdAccountSync,
                transport.Json,
                tenantId,
                DateTime.UtcNow).Value;
        published.MarkInProgress(DateTime.UtcNow);
        published.MarkPublished();

        var outboxStore = Substitute.For<IIntegrationOutboxStore>();
        outboxStore.GetByEventIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(published);
        outboxStore.UpdateAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var consumer = BuildConsumer(inbox, executor, sender, outboxStore);
        await consumer.Consume(CreateContext(transport));

        await inbox.DidNotReceive().TryRecordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await outboxStore.Received(1).UpdateAsync(Arg.Is<IntegrationOutboxMessage>(m => m.Status == IntegrationOutboxMessageStatus.Pending), Arg.Any<CancellationToken>());
    }

    /// <summary>Erro de validação dá ack e grava inbox.</summary>
    [Fact]
    public async Task Consume_WhenValidationError_AcknowledgesAndRecordsInbox()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var transport = CreateTransport(tenantId, workspaceId, accountId, eventId);

        var inbox = Substitute.For<IIntegrationInboxStore>();
        inbox.ExistsAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<CancellationToken>()).Returns(false);
        inbox.TryRecordAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetConnectedAdAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ConnectedAdAccountDto>>.Success(Array.Empty<ConnectedAdAccountDto>()));

        var executor = Substitute.For<IConnectedAdAccountMetaSyncExecutor>();

        var consumer = BuildConsumer(inbox, executor, sender, Substitute.For<IIntegrationOutboxStore>());
        await consumer.Consume(CreateContext(transport));

        await executor.DidNotReceive().ExecuteAsync(Arg.Any<Guid>(), Arg.Any<ConnectedAdAccountDto>(), Arg.Any<CancellationToken>());
        await inbox.Received(1).TryRecordAsync(eventId, MediaSyncIntegrationConsumer.ConsumerName, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private static IntegrationEventTransportMessage CreateTransport(Guid tenantId, Guid workspaceId, Guid accountId, Guid eventId)
    {
        using var payloadDoc = JsonDocument.Parse(
            $$"""{"workspaceId":"{{workspaceId}}","connectedAdAccountId":"{{accountId}}","platform":"MetaAds"}""");

        var envelope = IntegrationEventEnvelope.Create(
            eventId,
            IntegrationsIntegrationMessageTypes.AdAccountSync,
            accountId,
            DateTimeOffset.UtcNow,
            IntegrationEventScope.Operational,
            tenantId,
            payloadDoc.RootElement.Clone()).Value;

        var json = IntegrationEventEnvelopeSerializer.Serialize(envelope);
        return new IntegrationEventTransportMessage
        {
            EventId = eventId,
            MessageType = IntegrationsIntegrationMessageTypes.AdAccountSync,
            TenantId = tenantId,
            Json = json
        };
    }

    private static MediaSyncIntegrationConsumer BuildConsumer(
        IIntegrationInboxStore inbox,
        IConnectedAdAccountMetaSyncExecutor executor,
        ISender sender,
        IIntegrationOutboxStore outboxStore)
    {
        var services = new ServiceCollection();
        services.AddSingleton(inbox);
        services.AddSingleton(executor);
        services.AddSingleton(sender);
        services.AddSingleton(outboxStore);
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        var provider = services.BuildServiceProvider();
        return new MediaSyncIntegrationConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MediaSyncConsumerOptions()));
    }

    private static ConsumeContext<IntegrationEventTransportMessage> CreateContext(IntegrationEventTransportMessage message)
    {
        var context = Substitute.For<ConsumeContext<IntegrationEventTransportMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}