using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;
using SocialPublishing.Application.Jobs;
using SocialPublishing.Application.Posts.Commands.GeneratePostImage;
using SocialPublishing.Application.Posts.DTOs;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor que executa <see cref="GeneratePostImageCommand"/> para jobs de imagem.
/// </summary>
public sealed class SocialPostImageJobConsumer(IServiceScopeFactory scopeFactory) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome estável do consumidor para inbox.
    /// </summary>
    public const string ConsumerName = "admetricspro.social.generate-image";

    private readonly UserInitiatedJobConsumerSupport _support = new(scopeFactory, ConsumerName);

    /// <inheritdoc />
    public Task Consume(ConsumeContext<IntegrationEventTransportMessage> context) =>
        _support.ConsumeAsync(
            context,
            UserInitiatedJobIntegrationMessageTypes.PostImageGenerate,
            async (services, job, ct) =>
            {
                var parameters = JsonSerializer.Deserialize<PostImageJobParameters>(job.ParametersJson)!;
                var sender = services.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new GeneratePostImageCommand(
                        parameters.WorkspaceId,
                        parameters.PostId,
                        parameters.Direction),
                    ct);

                if (result.IsFailure)
                {
                    return (BuildingBlocks.Domain.Primitives.Result.Failure(result.Error), null);
                }

                var payload = JsonSerializer.Serialize(new { postId = result.Value.Id });
                return (BuildingBlocks.Domain.Primitives.Result.Success(), payload);
            },
            context.CancellationToken);
}