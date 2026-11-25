using FluentAssertions;
using MassTransit;
using Master.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using WebApi.Background;

namespace AcceptanceTests.Composition;

/// <summary>
/// Valida o registro de serviços por <c>Messaging:HostRole</c> (Fase 10).
/// </summary>
public sealed class MessagingHostCompositionTests
{
    /// <summary>
    /// Papéis efetivos registram workers e bus conforme matriz.
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <param name="expectSchedulers">Espera laços temporais.</param>
    /// <param name="expectBus">Espera MassTransit.</param>
    [Theory]
    [InlineData("Api", false, false)]
    [InlineData("Consumer", false, true)]
    [InlineData("Scheduler", true, true)]
    [InlineData("All", true, true)]
    public void HostRole_RegistersExpectedWorkersAndBus(string role, bool expectSchedulers, bool expectBus)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Messaging:Enabled", "true");
            builder.UseSetting("Messaging:HostRole", role);
            builder.UseSetting("RabbitMQ:Host", "127.0.0.1");
            builder.UseSetting("RabbitMQ:Port", "5672");
            builder.UseSetting("RabbitMQ:Username", "guest");
            builder.UseSetting("RabbitMQ:Password", "guest");
            builder.UseSetting("RabbitMQ:VirtualHost", "admetricspro");
        });

        var services = factory.Services;
        HasHostedService<OutboxDispatchWorker>(services).Should().Be(expectSchedulers);
        HasHostedService<MetaSynchronizationWorker>(services).Should().Be(expectSchedulers);
        HasHostedService<SocialPublishingDispatchWorker>(services).Should().Be(expectSchedulers);
        HasHostedService<DunningBackgroundService>(services).Should().Be(expectSchedulers);
        HasHostedService<TrialNoticeBackgroundService>(services).Should().Be(expectSchedulers);

        var bus = services.GetService<IBus>();
        if (expectBus)
        {
            bus.Should().NotBeNull();
        }
        else
        {
            bus.Should().BeNull();
        }
    }

    private static bool HasHostedService<T>(IServiceProvider services)
        where T : class, IHostedService
    {
        return services.GetServices<IHostedService>().Any(h => h is T);
    }
}