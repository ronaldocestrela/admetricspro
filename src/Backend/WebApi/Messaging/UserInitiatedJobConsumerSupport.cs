using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Consumers;
using MassTransit;
using Tenants.Application.Persistence;

namespace WebApi.Messaging;

/// <summary>
/// Fluxo comum de consumo: tenant, inbox, estado do job e ack.
/// </summary>
public sealed class UserInitiatedJobConsumerSupport(
    IServiceScopeFactory scopeFactory,
    string consumerName)
{
    /// <summary>
    /// Executa o trabalho de um job e atualiza estado terminal.
    /// </summary>
    /// <param name="context">Contexto MassTransit.</param>
    /// <param name="messageType">Tipo esperado.</param>
    /// <param name="executeAsync">Delegado que retorna resultado serializável ou erro.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    public async Task ConsumeAsync(
        ConsumeContext<IntegrationEventTransportMessage> context,
        string messageType,
        Func<IServiceProvider, UserInitiatedJob, CancellationToken, Task<(BuildingBlocks.Domain.Primitives.Result Result, string? ResultJson)>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(context.Message.MessageType, messageType, StringComparison.Ordinal))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(context.Message.Json);
        if (envelopeResult.IsFailure)
        {
            throw new IntegrationMessageRejectedException(envelopeResult.Error.Code);
        }

        var envelope = envelopeResult.Value;
        if (!envelope.TenantId.HasValue)
        {
            throw new IntegrationMessageRejectedException(MessagingErrors.TenantRequired.Code);
        }

        services.GetRequiredService<BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor>().TenantContext =
            BuildingBlocks.Infrastructure.MultiTenancy.TenantContext.Create(
                envelope.TenantId.Value,
                subdomain: null,
                BuildingBlocks.Application.MultiTenancy.TenantResolutionSource.None);

        var inbox = services.GetRequiredService<IIntegrationInboxStore>();
        if (await inbox.ExistsAsync(envelope.EventId, consumerName, cancellationToken))
        {
            return;
        }

        var jobId = envelope.Payload.GetProperty("jobId").GetGuid();
        var jobStore = services.GetRequiredService<IUserInitiatedJobStore>();
        var job = await jobStore.GetByIdForExecutionAsync(jobId, cancellationToken);
        if (job is null)
        {
            throw new IntegrationMessageRejectedException(MessagingErrors.PayloadInvalid.Code);
        }

        if (job.IsTerminal)
        {
            await inbox.TryRecordAsync(envelope.EventId, consumerName, DateTime.UtcNow, cancellationToken);
            await services.GetRequiredService<ITenantUnitOfWork>().CommitAsync(cancellationToken);
            return;
        }

        var utcNow = DateTime.UtcNow;
        _ = job.MarkRunning(utcNow);
        await jobStore.UpdateAsync(job, cancellationToken);

        var (result, resultJson) = await executeAsync(services, job, cancellationToken);
        var disposition = IntegrationResultDisposition.ForResult(result, alreadyInInbox: false);

        if (disposition.Action == IntegrationMessageDispositionAction.Reschedule)
        {
            throw new InvalidOperationException("Falha transitória de job de usuário.");
        }

        if (result.IsSuccess)
        {
            job.MarkSucceeded(resultJson, utcNow);
        }
        else if (disposition.Action == IntegrationMessageDispositionAction.Acknowledge)
        {
            job.MarkFailed(result.Error, utcNow);
        }
        else
        {
            throw new InvalidOperationException("Falha inesperada no job de usuário.");
        }

        await jobStore.UpdateAsync(job, cancellationToken);
        await inbox.TryRecordAsync(envelope.EventId, consumerName, DateTime.UtcNow, cancellationToken);
        await services.GetRequiredService<ITenantUnitOfWork>().CommitAsync(cancellationToken);
    }
}