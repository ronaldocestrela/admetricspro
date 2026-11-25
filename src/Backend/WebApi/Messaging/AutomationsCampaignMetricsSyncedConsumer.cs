using Automations.Application.Ingestion.Commands.ReactToCampaignMetricsSynced;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;
using WebApi.Configuration;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor de reações de automação após ingestão durável de métricas.
/// </summary>
public sealed class AutomationsCampaignMetricsSyncedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<IngestionReactionsConsumerOptions> consumerOptions) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome da fila e do consumidor para inbox.
    /// </summary>
    public const string ConsumerName = "admetricspro.automations.campaign-metrics-synced";

    /// <inheritdoc />
    public Task Consume(ConsumeContext<IntegrationEventTransportMessage> context) =>
        IngestionReactionIntegrationConsumer.ConsumeAsync(
            context,
            scopeFactory,
            ConsumerName,
            IntegrationsIntegrationMessageTypes.CampaignMetricsSynced,
            CreateCommand,
            consumerOptions);

    private static IRequest<Result> CreateCommand(IntegrationEventEnvelope envelope)
    {
        var workspaceId = envelope.Payload.GetProperty("workspaceId").GetGuid();
        return new ReactToCampaignMetricsSyncedCommand(workspaceId);
    }
}