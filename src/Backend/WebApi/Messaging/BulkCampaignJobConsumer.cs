using System.Text.Json;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.Contracts;
using Integrations.Application.Jobs;
using MassTransit;
using MediatR;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor que executa um lote de campanhas previamente aceito.
/// </summary>
public sealed class BulkCampaignJobConsumer(IServiceScopeFactory scopeFactory) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome do consumidor de mutação em massa.
    /// </summary>
    public const string ConsumerName = "admetricspro.integrations.bulk-campaign-mutation";

    private readonly UserInitiatedJobConsumerSupport _support = new(scopeFactory, ConsumerName);

    /// <inheritdoc />
    public Task Consume(ConsumeContext<IntegrationEventTransportMessage> context) =>
        _support.ConsumeAsync(
            context,
            UserInitiatedJobIntegrationMessageTypes.BulkCampaignMutation,
            async (services, job, ct) =>
            {
                var parameters = JsonSerializer.Deserialize<BulkCampaignJobParameters>(job.ParametersJson)!;
                var sender = services.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new BulkCampaignOperationCommand(parameters.WorkspaceId, parameters.Operations),
                    ct);

                if (result.IsFailure)
                {
                    return (BuildingBlocks.Domain.Primitives.Result.Failure(result.Error), null);
                }

                return (BuildingBlocks.Domain.Primitives.Result.Success(), JsonSerializer.Serialize(result.Value));
            },
            context.CancellationToken);
}