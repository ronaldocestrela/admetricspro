using BuildingBlocks.Application.Messaging;
using MassTransit;
using SocialPublishing.Application.Messaging;
using WebApi.Configuration;
using WebApi.Messaging;

namespace WebApi.Extensions;

/// <summary>
/// Registro centralizado de consumidores MassTransit da WebApi.
/// </summary>
public static class AdMetricsMessagingConsumerRegistration
{
    /// <summary>
    /// Adiciona consumidores ao configurador de registro MassTransit.
    /// </summary>
    /// <param name="bus">Configurador de registro.</param>
    public static void AddWebApiIntegrationConsumers(IRegistrationConfigurator bus)
    {
        bus.AddConsumer<CatalogEmailIntegrationConsumer>();
        bus.AddConsumer<MediaSyncIntegrationConsumer, MediaSyncConsumerDefinition>();
        bus.AddConsumer<AutomationsCampaignMetricsSyncedConsumer>();
        bus.AddConsumer<AutomationsCampaignHierarchySyncedConsumer>();
        bus.AddConsumer<AnalyticsCampaignMetricsSyncedConsumer>();
        bus.AddConsumer<AnalyticsCampaignHierarchySyncedConsumer>();
        bus.AddConsumer<PublishOrganicSlotIntegrationConsumer>();
        bus.AddConsumer<SocialEditorialPlanJobConsumer>();
        bus.AddConsumer<SocialPostImageJobConsumer>();
        bus.AddConsumer<AnalyticsReportJobConsumer>();
        bus.AddConsumer<BulkCampaignJobConsumer>();
    }

    /// <summary>
    /// Configura filas, binds e prefetch dos consumidores de integração.
    /// </summary>
    /// <param name="context">Contexto de registro do bus.</param>
    /// <param name="cfg">Configurador RabbitMQ.</param>
    public static void ConfigureWebApiReceiveEndpoints(
        IBusRegistrationContext context,
        IRabbitMqBusFactoryConfigurator cfg)
    {
        var mediaSyncOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<MediaSyncConsumerOptions>>().Value;
        var mediaPrefetch = (ushort)Math.Clamp((int)mediaSyncOptions.Prefetch, 1, 4);
        var ingestionOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<IngestionReactionsConsumerOptions>>().Value;
        var ingestionPrefetch = (ushort)Math.Clamp((int)ingestionOptions.Prefetch, 1, 4);
        var organicPublishOptions = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrganicPublishConsumerOptions>>().Value;
        var organicPrefetch = (ushort)Math.Clamp((int)organicPublishOptions.Prefetch, 1, 4);

        cfg.ReceiveEndpoint(CatalogEmailIntegrationConsumer.ConsumerName, endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = CatalogIntegrationMessageTypes.TenantWelcomeEmail;
            });
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = CatalogIntegrationMessageTypes.SubscriptionConfirmationEmail;
            });
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = CatalogIntegrationMessageTypes.TrialNoticeEmail;
            });
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = CatalogIntegrationMessageTypes.DunningStageEmail;
            });
            endpoint.ConfigureConsumer<CatalogEmailIntegrationConsumer>(context);
        });

        cfg.ReceiveEndpoint(MediaSyncIntegrationConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = mediaPrefetch;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = IntegrationsIntegrationMessageTypes.AdAccountSync;
            });
            endpoint.ConfigureConsumer<MediaSyncIntegrationConsumer>(context);
        });

        cfg.ReceiveEndpoint(AutomationsCampaignMetricsSyncedConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = ingestionPrefetch;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = IntegrationsIntegrationMessageTypes.CampaignMetricsSynced;
            });
            endpoint.ConfigureConsumer<AutomationsCampaignMetricsSyncedConsumer>(context);
        });

        cfg.ReceiveEndpoint(AutomationsCampaignHierarchySyncedConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = ingestionPrefetch;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = IntegrationsIntegrationMessageTypes.CampaignHierarchySynced;
            });
            endpoint.ConfigureConsumer<AutomationsCampaignHierarchySyncedConsumer>(context);
        });

        cfg.ReceiveEndpoint(AnalyticsCampaignMetricsSyncedConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = ingestionPrefetch;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = IntegrationsIntegrationMessageTypes.CampaignMetricsSynced;
            });
            endpoint.ConfigureConsumer<AnalyticsCampaignMetricsSyncedConsumer>(context);
        });

        cfg.ReceiveEndpoint(AnalyticsCampaignHierarchySyncedConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = ingestionPrefetch;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = IntegrationsIntegrationMessageTypes.CampaignHierarchySynced;
            });
            endpoint.ConfigureConsumer<AnalyticsCampaignHierarchySyncedConsumer>(context);
        });

        cfg.ReceiveEndpoint(PublishOrganicSlotIntegrationConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = organicPrefetch;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = SocialPublishingIntegrationMessageTypes.PublishOrganicSlot;
            });
            endpoint.ConfigureConsumer<PublishOrganicSlotIntegrationConsumer>(context);
        });

        cfg.ReceiveEndpoint(SocialEditorialPlanJobConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = 1;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = UserInitiatedJobIntegrationMessageTypes.EditorialPlanGenerate;
            });
            endpoint.ConfigureConsumer<SocialEditorialPlanJobConsumer>(context);
        });

        cfg.ReceiveEndpoint(SocialPostImageJobConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = 1;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = UserInitiatedJobIntegrationMessageTypes.PostImageGenerate;
            });
            endpoint.ConfigureConsumer<SocialPostImageJobConsumer>(context);
        });

        cfg.ReceiveEndpoint(AnalyticsReportJobConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = 1;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = UserInitiatedJobIntegrationMessageTypes.ReportGenerate;
            });
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = UserInitiatedJobIntegrationMessageTypes.ReportDispatch;
            });
            endpoint.ConfigureConsumer<AnalyticsReportJobConsumer>(context);
        });

        cfg.ReceiveEndpoint(BulkCampaignJobConsumer.ConsumerName, endpoint =>
        {
            endpoint.PrefetchCount = 1;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind("admetricspro.integration", bind =>
            {
                bind.ExchangeType = "topic";
                bind.RoutingKey = UserInitiatedJobIntegrationMessageTypes.BulkCampaignMutation;
            });
            endpoint.ConfigureConsumer<BulkCampaignJobConsumer>(context);
        });
    }
}