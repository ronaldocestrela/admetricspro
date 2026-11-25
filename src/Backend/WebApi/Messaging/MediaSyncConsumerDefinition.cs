using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;

namespace WebApi.Messaging;

/// <summary>
/// Define endpoint quorum, prefetch e bind do consumidor de sync de mídia.
/// </summary>
public sealed class MediaSyncConsumerDefinition : ConsumerDefinition<MediaSyncIntegrationConsumer>
{
    /// <summary>
    /// Inicializa fila e binding para <see cref="IntegrationsIntegrationMessageTypes.AdAccountSync"/>.
    /// </summary>
    public MediaSyncConsumerDefinition()
    {
        EndpointName = MediaSyncIntegrationConsumer.ConsumerName;
    }

    /// <inheritdoc />
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<MediaSyncIntegrationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.ConfigureConsumeTopology = false;
            rabbit.Bind("admetricspro.integration", bind =>
            {
                bind.RoutingKey = IntegrationsIntegrationMessageTypes.AdAccountSync;
                bind.ExchangeType = "topic";
            });
        }
    }
}