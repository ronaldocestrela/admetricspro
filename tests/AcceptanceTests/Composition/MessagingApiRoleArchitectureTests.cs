using BuildingBlocks.Application.Messaging.Transport;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.Composition;

/// <summary>
/// Papel <c>Api</c> grava outbox sem MassTransit nem health check do broker.
/// </summary>
public sealed class MessagingApiRoleArchitectureTests
{
    /// <summary>
    /// Host Api sobe sem bus e mantém /health 200.
    /// </summary>
    [Fact]
    public async Task ApiRole_WithMessagingEnabled_DoesNotRegisterBusOrRabbitHealth()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Messaging:Enabled", "true");
            builder.UseSetting("Messaging:HostRole", "Api");
            builder.UseSetting("RabbitMQ:Host", "127.0.0.1");
            builder.UseSetting("RabbitMQ:Port", "1");
        });

        factory.Services.GetService<IBus>().Should().BeNull();
        factory.Services.GetService<IIntegrationEventPublisher>().Should().NotBeNull();

        var response = await factory.CreateClient().GetAsync("/health");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}