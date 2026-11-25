using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Persistence;
using Master.Application.DependencyInjection;
using BuildingBlocks.Messaging.Consumers;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.DependencyInjection;
using FluentAssertions;
using MassTransit;
using IntegrationTests.Infrastructure;
using IntegrationTests.Messaging.Laboratory;
using Master.Domain.Tenants;
using Master.Infrastructure.Extensions;
using Master.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApi.Background;
using WebApi.Configuration;

namespace IntegrationTests.Messaging;

/// <summary>
/// Circuito SQL → dispatcher → RabbitMQ → consumidor ping idempotente.
/// </summary>
[Collection(MessagingSqlRabbitCollection.Name)]
public sealed class PingIntegrationCircuitTests(SqlServerFixture sqlFixture, RabbitMqFixture rabbitFixture)
{
    /// <summary>
    /// Uma entrega do mesmo EventId não duplica o efeito colateral.
    /// </summary>
    [Fact]
    public async Task OperationalPing_ReentregaNaoDuplicaEfeito()
    {
        var eventId = Guid.NewGuid();
        var pingId = Guid.NewGuid();

        var masterCatalogDb = $"Master_Ping_{Guid.NewGuid():N}";
        var masterConnection = new SqlConnectionStringBuilder(sqlFixture.ConnectionString)
        {
            InitialCatalog = masterCatalogDb,
        }.ConnectionString;

        var tenantDb = $"Tenant_Ping_{Guid.NewGuid():N}";
        var tenantConnection = new SqlConnectionStringBuilder(sqlFixture.ConnectionString)
        {
            InitialCatalog = tenantDb,
        }.ConnectionString;

        Guid tenantId;
        await using (var masterSeed = CreateMasterContext(masterConnection))
        {
            await masterSeed.Database.MigrateAsync();
            var tenant = Tenant.Create("Ping Lab", "12345678901234", "pinglab").Value;
            tenantId = tenant.Id.Value;
            masterSeed.Tenants.Add(tenant);
            await masterSeed.SaveChangesAsync();
        }

        await using (var tenantSeed = CreateTenantContext(tenantConnection))
        {
            await tenantSeed.Database.MigrateAsync();
            tenantSeed.IntegrationOutboxMessages.Add(PingOutboxFactory.CreateOutboxLine(eventId, tenantId, pingId));
            await tenantSeed.SaveChangesAsync();
        }

        var effectStore = new InMemoryPingIntegrationEffectStore();
        var connectionMap = new Dictionary<Guid, string> { [tenantId] = tenantConnection };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Enabled"] = "true",
                ["Messaging:Dispatch:Enabled"] = "true",
                ["Messaging:Dispatch:IntervalSeconds"] = "5",
                ["RabbitMQ:Host"] = rabbitFixture.HostName,
                ["RabbitMQ:Port"] = rabbitFixture.AmqpPort.ToString(),
                ["RabbitMQ:VirtualHost"] = "/",
                ["RabbitMQ:Username"] = rabbitFixture.Username,
                ["RabbitMQ:Password"] = rabbitFixture.Password,
            })
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddLogging();
                services.AddMultiTenancy();
                services.AddMasterCatalog(masterConnection);
                services.AddSingleton<ITenantConnectionResolver>(sp =>
                    new AccessorBackedTenantConnectionResolver(
                        sp.GetRequiredService<ITenantContextAccessor>(),
                        connectionMap));
                services.AddMasterApplication();
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PingIntegrationCommand).Assembly));
                services.AddTenantPersistence<BuildingBlocks.Infrastructure.Persistence.TenantDbContext>();
                services.AddTenantMessagingStores();
                services.AddMessagingSchedulerLeadership(configuration);
                services.AddSingleton<IPingIntegrationEffectStore>(effectStore);
                services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
                services.Configure<OutboxDispatchOptions>(configuration.GetSection(OutboxDispatchOptions.SectionName));
                services.AddAdMetricsMessaging(
                    configuration,
                    BuildingBlocks.Messaging.DependencyInjection.AdMetricsMessagingRegistrationMode.Full,
                    bus => bus.AddConsumer<PingIntegrationMessageConsumer>(),
                    (context, cfg) =>
                    {
                        cfg.ReceiveEndpoint("admetricspro.messaging.ping", endpoint =>
                        {
                            endpoint.ConfigureConsumeTopology = false;
                            endpoint.Bind("admetricspro.integration", bind =>
                            {
                                bind.ExchangeType = "topic";
                                bind.RoutingKey = "messaging.ping.v1";
                            });
                            endpoint.ConfigureConsumer<PingIntegrationMessageConsumer>(context);
                        });
                    });
                services.AddSingleton<OutboxDispatchWorker>();
            })
            .Build();

        await host.StartAsync();

        try
        {
            var worker = host.Services.GetRequiredService<OutboxDispatchWorker>();
            await worker.RunCycleAsync(CancellationToken.None);

            await using (var verifyPublish = CreateTenantContext(tenantConnection))
            {
                var published = await verifyPublish.IntegrationOutboxMessages.AsNoTracking()
                    .SingleAsync(m => m.EventId == eventId);
                published.Status.Should().Be(IntegrationOutboxMessageStatus.Published, "dispatcher deve publicar a outbox do tenant");
            }

            await WaitForEffectAsync(effectStore, eventId, TimeSpan.FromSeconds(90));

            effectStore.GetCount(eventId).Should().Be(1);

            await using var tenantVerify = CreateTenantContext(tenantConnection);
            var inbox = await tenantVerify.IntegrationInboxMessages
                .AsNoTracking()
                .SingleOrDefaultAsync(i => i.EventId == eventId && i.ConsumerName == PingIntegrationMessageConsumer.ConsumerName);
            inbox.Should().NotBeNull();

            var outboxLine = await tenantVerify.IntegrationOutboxMessages.AsNoTracking().SingleAsync(m => m.EventId == eventId);
            outboxLine.Status.Should().Be(IntegrationOutboxMessageStatus.Published);

            var publishEndpoint = host.Services.GetRequiredService<IPublishEndpoint>();
            var redelivery = new IntegrationEventTransportMessage
            {
                Json = outboxLine.PayloadJson,
                MessageType = outboxLine.MessageType,
                EventId = eventId,
                TenantId = tenantId,
            };
            await publishEndpoint.Publish(
                redelivery,
                context =>
                {
                    context.SetRoutingKey(outboxLine.MessageType);
                    context.Headers.Set("message-id", eventId.ToString());
                    context.Headers.Set("message-type", outboxLine.MessageType);
                });

            await Task.Delay(TimeSpan.FromSeconds(5));
            effectStore.GetCount(eventId).Should().Be(1, "inbox deve ack idempotente na reentrega");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static async Task WaitForEffectAsync(
        InMemoryPingIntegrationEffectStore effectStore,
        Guid eventId,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (effectStore.GetCount(eventId) > 0)
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException("Consumidor ping não processou a mensagem a tempo.");
    }

    private static MasterDbContext CreateMasterContext(string connectionString) =>
        new(new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

    private static TenantOperationalDbContext CreateTenantContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlServer(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

}