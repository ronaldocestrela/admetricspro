using Analytics.Application.Ingestion.Commands.ProcessCampaignHierarchySynced;
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
/// Consumidor analítico após sincronização durável de hierarquia (taxonomia em memória).
/// </summary>
public sealed class AnalyticsCampaignHierarchySyncedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<IngestionReactionsConsumerOptions> consumerOptions) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome da fila e do consumidor para inbox.
    /// </summary>
    public const string ConsumerName = "admetricspro.analytics.campaign-hierarchy-synced";

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
        var accountId = envelope.Payload.GetProperty("connectedAdAccountId").GetGuid();
        return new ProcessCampaignHierarchySyncedCommand(workspaceId, accountId);
    }
}