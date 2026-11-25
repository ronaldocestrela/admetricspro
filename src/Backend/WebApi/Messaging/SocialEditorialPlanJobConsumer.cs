using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;
using SocialPublishing.Application.Jobs;
using SocialPublishing.Application.Planning.Commands.GenerateEditorialPlan;
using SocialPublishing.Application.Planning.DTOs;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor que executa <see cref="GenerateEditorialPlanCommand"/> para jobs de plano editorial.
/// </summary>
public sealed class SocialEditorialPlanJobConsumer(IServiceScopeFactory scopeFactory) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome estável do consumidor para inbox.
    /// </summary>
    public const string ConsumerName = "admetricspro.social.generate-plan";

    private readonly UserInitiatedJobConsumerSupport _support = new(scopeFactory, ConsumerName);

    /// <inheritdoc />
    public Task Consume(ConsumeContext<IntegrationEventTransportMessage> context) =>
        _support.ConsumeAsync(
            context,
            UserInitiatedJobIntegrationMessageTypes.EditorialPlanGenerate,
            async (services, job, ct) =>
            {
                var parameters = JsonSerializer.Deserialize<EditorialPlanJobParameters>(job.ParametersJson)!;
                var sender = services.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new GenerateEditorialPlanCommand(
                        parameters.WorkspaceId,
                        parameters.RangeStart,
                        parameters.RangeEnd,
                        parameters.TimeZoneId,
                        parameters.PostsPerWeek,
                        parameters.Seed),
                    ct);

                if (result.IsFailure)
                {
                    return (BuildingBlocks.Domain.Primitives.Result.Failure(result.Error), null);
                }

                var json = JsonSerializer.Serialize(result.Value);
                return (BuildingBlocks.Domain.Primitives.Result.Success(), json);
            },
            context.CancellationToken);
}