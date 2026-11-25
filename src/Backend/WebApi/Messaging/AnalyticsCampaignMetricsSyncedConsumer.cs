using Analytics.Application.Ingestion.Commands.ProcessCampaignMetricsSynced;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Options;
using WebApi.Configuration;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor analítico após ingestão durável de métricas (câmbio em memória).
/// </summary>
public sealed class AnalyticsCampaignMetricsSyncedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<IngestionReactionsConsumerOptions> consumerOptions) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome da fila e do consumidor para inbox.
    /// </summary>
    public const string ConsumerName = "admetricspro.analytics.campaign-metrics-synced";

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
        var payload = envelope.Payload;
        var workspaceId = payload.GetProperty("workspaceId").GetGuid();
        var accountId = payload.GetProperty("connectedAdAccountId").GetGuid();
        var granularity = Enum.Parse<MetricGranularity>(payload.GetProperty("granularity").GetString()!, ignoreCase: true);
        var windowStart = payload.GetProperty("windowStartUtc").GetDateTime();
        var windowEnd = payload.GetProperty("windowEndUtc").GetDateTime();
        return new ProcessCampaignMetricsSyncedCommand(workspaceId, accountId, granularity, windowStart, windowEnd);
    }
}