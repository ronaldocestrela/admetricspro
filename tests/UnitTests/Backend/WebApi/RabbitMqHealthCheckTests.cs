using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApi.Extensions;

namespace UnitTests.Backend.HealthChecks;

/// <summary>
/// Testes unitários do health check condicional do broker RabbitMQ (Fase 1).
/// </summary>
public sealed class RabbitMqHealthCheckTests
{
    /// <summary>
    /// Com mensageria desligada, o check <c>rabbitmq</c> não é registrado e o agregado permanece saudável.
    /// </summary>
    [Fact]
    public async Task WhenMessagingDisabled_ShouldNotRegisterRabbitMqCheck_AndReportHealthy()
    {
        var services = CreateHealthCheckServices(new Dictionary<string, string?>
        {
            ["Messaging:Enabled"] = "false",
        });

        await AssertHealthAsync(services, expectRabbitMqEntry: false, expectedStatus: HealthStatus.Healthy);
    }

    /// <summary>
    /// Sem a chave <c>Messaging:Enabled</c>, o comportamento equivale a desligado (compatível com o deploy atual).
    /// </summary>
    [Fact]
    public async Task WhenMessagingEnabledKeyAbsent_ShouldNotRegisterRabbitMqCheck_AndReportHealthy()
    {
        var services = CreateHealthCheckServices(new Dictionary<string, string?>());

        await AssertHealthAsync(services, expectRabbitMqEntry: false, expectedStatus: HealthStatus.Healthy);
    }

    /// <summary>
    /// Com mensageria ligada e broker inacessível, o check nomeado <c>rabbitmq</c> fica <see cref="HealthStatus.Unhealthy"/>.
    /// </summary>
    [Fact]
    public async Task WhenMessagingEnabledAndBrokerUnreachable_ShouldReportRabbitMqUnhealthy()
    {
        var services = CreateHealthCheckServices(new Dictionary<string, string?>
        {
            ["Messaging:Enabled"] = "true",
            ["RabbitMQ:Host"] = "127.0.0.1",
            ["RabbitMQ:Port"] = "59672",
            ["RabbitMQ:VirtualHost"] = "admetricspro",
            ["RabbitMQ:Username"] = "admetrics",
            ["RabbitMQ:Password"] = "dev-only-password",
        });

        await AssertHealthAsync(services, expectRabbitMqEntry: true, expectedStatus: HealthStatus.Unhealthy);
    }

    private static ServiceProvider CreateHealthCheckServices(IReadOnlyDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddRabbitMqHealthCheck(configuration);

        return services.BuildServiceProvider();
    }

    private static async Task AssertHealthAsync(
        ServiceProvider provider,
        bool expectRabbitMqEntry,
        HealthStatus expectedStatus)
    {
        await using (provider)
        {
            var healthCheckService = provider.GetRequiredService<HealthCheckService>();
            var report = await healthCheckService.CheckHealthAsync();

            report.Status.Should().Be(expectedStatus);

            report.Entries.ContainsKey("rabbitmq").Should().Be(expectRabbitMqEntry);
            if (expectRabbitMqEntry)
            {
                report.Entries["rabbitmq"].Status.Should().Be(HealthStatus.Unhealthy);
            }
        }
    }
}