using BuildingBlocks.Messaging.Contracts;
using MassTransit;

namespace WebApi.Messaging;

/// <summary>
/// Define endpoint quorum e binds do consumidor de e-mail transacional do catálogo.
/// </summary>
public sealed class CatalogEmailConsumerDefinition : ConsumerDefinition<CatalogEmailIntegrationConsumer>
{
    /// <summary>
    /// Inicializa fila e bindings para os tipos de e-mail do Master.
    /// </summary>
    public CatalogEmailConsumerDefinition()
    {
        EndpointName = CatalogEmailIntegrationConsumer.ConsumerName;
    }

    /// <inheritdoc />
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CatalogEmailIntegrationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.ConfigureConsumeTopology = false;
            rabbit.Bind("admetricspro.integration", bind =>
            {
                bind.RoutingKey = BuildingBlocks.Application.Messaging.CatalogIntegrationMessageTypes.TenantWelcomeEmail;
                bind.ExchangeType = "topic";
            });
            rabbit.Bind("admetricspro.integration", bind =>
            {
                bind.RoutingKey = BuildingBlocks.Application.Messaging.CatalogIntegrationMessageTypes.SubscriptionConfirmationEmail;
                bind.ExchangeType = "topic";
            });
        }
    }
}