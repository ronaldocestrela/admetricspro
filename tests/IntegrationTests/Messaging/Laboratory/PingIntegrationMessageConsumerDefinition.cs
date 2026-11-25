using MassTransit;

namespace IntegrationTests.Messaging.Laboratory;

/// <summary>
/// Topologia de bind do consumidor ping no exchange <c>admetricspro.integration</c>.
/// </summary>
public sealed class PingIntegrationMessageConsumerDefinition : ConsumerDefinition<PingIntegrationMessageConsumer>
{
    /// <summary>
    /// Inicializa o endpoint com nome fixo e binding por routing key.
    /// </summary>
    public PingIntegrationMessageConsumerDefinition()
    {
        EndpointName = "admetricspro.messaging.ping";
    }

    /// <inheritdoc />
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PingIntegrationMessageConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.ConfigureConsumeTopology = false;

        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = "messaging.ping.v1";
            });
        }
    }
}