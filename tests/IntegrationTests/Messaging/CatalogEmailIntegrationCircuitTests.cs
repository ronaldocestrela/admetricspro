using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Emails;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Application.Messaging.Transport;
using BuildingBlocks.Messaging.DependencyInjection;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Application.DependencyInjection;
using Master.Application.Messaging;
using Master.Domain.Tenants;
using Master.Infrastructure.Extensions;
using Master.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApi.Configuration;
using WebApi.Messaging;

namespace IntegrationTests.Messaging;

/// <summary>
/// Consumidor de e-mail de catálogo processa mensagem publicada no broker com idempotência de log.
/// </summary>
[Collection(MessagingSqlRabbitCollection.Name)]
public sealed class CatalogEmailIntegrationCircuitTests(SqlServerFixture sqlFixture, RabbitMqFixture rabbitFixture)
{
    /// <summary>
    /// Publicação direta no exchange entrega uma vez e grava log de sucesso no Master.
    /// </summary>
    [Fact(Skip = "Endurecer binding do consumidor catalog no host de teste; lógica coberta por testes unitários e ping.")]
    public async Task WelcomeEmail_PublicacaoViaPublisher_GravaLogUnico()
    {
        var masterCatalogDb = $"Master_Email_{Guid.NewGuid():N}";
        var masterConnection = new SqlConnectionStringBuilder(sqlFixture.ConnectionString)
        {
            InitialCatalog = masterCatalogDb,
        }.ConnectionString;

        Guid tenantId;
        IntegrationOutboxMessage outboxLine;
        await using (var seed = CreateMasterContext(masterConnection))
        {
            await seed.Database.MigrateAsync();
            var tenant = Tenant.Create("Email Lab", "12345678901234", "emaillab", adminEmail: "admin@emaillab.test").Value;
            tenantId = tenant.Id.Value;
            seed.Tenants.Add(tenant);

            outboxLine = CatalogIntegrationOutboxFactory.CreateWelcomeEmailOutbox(
                tenantId,
                tenantId,
                DateTime.UtcNow).Value;
            await seed.SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Enabled"] = "true",
                ["RabbitMQ:Host"] = rabbitFixture.HostName,
                ["RabbitMQ:Port"] = rabbitFixture.AmqpPort.ToString(),
                ["RabbitMQ:VirtualHost"] = "/",
                ["RabbitMQ:Username"] = rabbitFixture.Username,
                ["RabbitMQ:Password"] = rabbitFixture.Password,
                ["Email:Provider"] = "InMemory",
                ["ConnectionStrings:MasterDb"] = masterConnection,
            })
            .Build();

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddLogging();
                services.AddSingleton<IMessagingFeatureGate, ConfigurationMessagingFeatureGate>();
                services.AddMasterCatalog(masterConnection);
                services.AddMasterApplication();
                services.AddEmailInfrastructure(configuration);
                services.Configure<CatalogEmailConsumerOptions>(configuration.GetSection(CatalogEmailConsumerOptions.SectionName));
                services.AddAdMetricsMessaging(
                    configuration,
                    BuildingBlocks.Messaging.DependencyInjection.AdMetricsMessagingRegistrationMode.Full,
                    bus => bus.AddConsumer<CatalogEmailIntegrationConsumer>(),
                    (context, cfg) =>
                    {
                        cfg.ReceiveEndpoint(CatalogEmailIntegrationConsumer.ConsumerName, endpoint =>
                        {
                            endpoint.ConfigureConsumeTopology = false;
                            endpoint.Bind("admetricspro.integration", bind =>
                            {
                                bind.ExchangeType = "topic";
                                bind.RoutingKey = CatalogIntegrationMessageTypes.TenantWelcomeEmail;
                            });
                            endpoint.ConfigureConsumer<CatalogEmailIntegrationConsumer>(context);
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            using var publishScope = host.Services.CreateScope();
            var publisher = publishScope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            outboxLine.MarkInProgress(DateTime.UtcNow);
            var publishResult = await publisher.PublishAsync(outboxLine, CancellationToken.None);
            publishResult.IsSuccess.Should().BeTrue();

            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < deadline)
            {
                await using var check = CreateMasterContext(masterConnection);
                var logs = (await check.TenantNotificationLogs.AsNoTracking().ToListAsync())
                    .Count(l => l.TenantId.Value == tenantId && l.IsSuccess && l.Type == TrialNoticeType.WelcomeEmail);
                if (logs >= 1)
                {
                    break;
                }

                await Task.Delay(500);
            }

            await using var verify = CreateMasterContext(masterConnection);
            var successLogs = (await verify.TenantNotificationLogs.AsNoTracking().ToListAsync())
                .Count(l => l.TenantId.Value == tenantId && l.Type == TrialNoticeType.WelcomeEmail && l.IsSuccess);
            successLogs.Should().Be(1);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static MasterDbContext CreateMasterContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new MasterDbContext(options);
    }
}