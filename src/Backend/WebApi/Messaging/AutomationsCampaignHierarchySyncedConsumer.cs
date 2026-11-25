using Automations.Application.Ingestion.Commands.ReactToCampaignHierarchySynced;
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
/// Consumidor de travas após sincronização durável de hierarquia.
/// </summary>
public sealed class AutomationsCampaignHierarchySyncedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<IngestionReactionsConsumerOptions> consumerOptions) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome da fila e do consumidor para inbox.
    /// </summary>
    public const string ConsumerName = "admetricspro.automations.campaign-hierarchy-synced";

    /// <inheritdoc />
    public Task Consume(ConsumeContext<IntegrationEventTransportMessage> context) =>
        IngestionReactionIntegrationConsumer.ConsumeAsync(
            context,
            scopeFactory,
            ConsumerName,
            IntegrationsIntegrationMessageTypes.CampaignHierarchySynced,
            CreateCommand,
            consumerOptions);

    private static IRequest<Result> CreateCommand(IntegrationEventEnvelope envelope)
    {
        var workspaceId = envelope.Payload.GetProperty("workspaceId").GetGuid();
        return new ReactToCampaignHierarchySyncedCommand(workspaceId);
    }
}