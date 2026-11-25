using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Consumers;
using BuildingBlocks.Messaging.Contracts;
using Integrations.Application.Messaging;
using MassTransit;
using Microsoft.Extensions.Options;
using Tenants.Application.Integrations.Queries.GetConnectedAdAccounts;
using WebApi.Background;
using WebApi.Configuration;

namespace WebApi.Messaging;

/// <summary>
/// Consumidor MassTransit de sincronização de conta de anúncio (hierarquia + métricas).
/// </summary>
public sealed class MediaSyncIntegrationConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<MediaSyncConsumerOptions> consumerOptions) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome estável do consumidor para inbox no banco do tenant.
    /// </summary>
    public const string ConsumerName = "admetricspro.integrations.media-sync";

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<IntegrationEventTransportMessage> context)
    {
        var messageType = context.Message.MessageType;
        if (!string.Equals(messageType, IntegrationsIntegrationMessageTypes.AdAccountSync, StringComparison.Ordinal))
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

        services.GetRequiredService<ITenantContextAccessor>().TenantContext = TenantContext.Create(
            envelope.TenantId.Value,
            subdomain: null,
            TenantResolutionSource.None);

        var inbox = services.GetRequiredService<IIntegrationInboxStore>();
        var alreadyInInbox = await inbox.ExistsAsync(envelope.EventId, ConsumerName, context.CancellationToken);
        if (alreadyInInbox)
        {
            return;
        }

        var workspaceId = envelope.Payload.GetProperty("workspaceId").GetGuid();
        var connectedAdAccountId = envelope.Payload.GetProperty("connectedAdAccountId").GetGuid();
        var platform = envelope.Payload.GetProperty("platform").GetString() ?? string.Empty;

        var sender = services.GetRequiredService<MediatR.ISender>();
        var accounts = await sender.Send(new GetConnectedAdAccountsQuery(workspaceId), context.CancellationToken);
        if (accounts.IsFailure)
        {
            var disposition = IntegrationResultDisposition.ForResult(accounts, alreadyInInbox: false);
            await HandleDispositionAsync(services, context, envelope.EventId, disposition, accounts, context.CancellationToken);
            return;
        }

        var account = accounts.Value.FirstOrDefault(a =>
            a.Id == connectedAdAccountId
            && a.WorkspaceId == workspaceId
            && string.Equals(a.Platform, platform, StringComparison.OrdinalIgnoreCase));

        if (account is null)
        {
            var notFound = Result.Failure(Error.NotFound("MetaSync.AccountNotFound", "Conta conectada não localizada para sincronização."));
            var disposition = IntegrationResultDisposition.ForResult(notFound, alreadyInInbox: false);
            await HandleDispositionAsync(services, context, envelope.EventId, disposition, notFound, context.CancellationToken);
            return;
        }

        var executor = services.GetRequiredService<IConnectedAdAccountMetaSyncExecutor>();
        var execution = await executor.ExecuteAsync(envelope.TenantId.Value, account, context.CancellationToken);
        var retryAfter = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, consumerOptions.Value.RetryAfterSeconds));
        var forDisposition = MediaSyncFailureMapper.ForDisposition(execution, retryAfter);
        var syncDisposition = IntegrationResultDisposition.ForResult(forDisposition, alreadyInInbox: false);
        await HandleDispositionAsync(services, context, envelope.EventId, syncDisposition, forDisposition, context.CancellationToken);
    }

    private async Task HandleDispositionAsync(
        IServiceProvider services,
        ConsumeContext<IntegrationEventTransportMessage> context,
        Guid eventId,
        IntegrationResultDisposition disposition,
        Result commandResult,
        CancellationToken cancellationToken)
    {
        MessagingConsumerMetrics.RecordDisposition(disposition.Action, context.Message.MessageType);
        if (disposition.Action == IntegrationMessageDispositionAction.Acknowledge)
        {
            await services.GetRequiredService<IIntegrationInboxStore>()
                .TryRecordAsync(eventId, ConsumerName, DateTime.UtcNow, cancellationToken);
            return;
        }

        if (disposition.Action == IntegrationMessageDispositionAction.RetryTransport)
        {
            throw new InvalidOperationException(
                commandResult.IsFailure
                    ? commandResult.Error.Description
                    : "Falha inesperada no consumidor de sync de mídia.");
        }

        if (disposition.Action == IntegrationMessageDispositionAction.Reschedule)
        {
            await ApplyConsumerRescheduleAsync(services, eventId, disposition, commandResult, cancellationToken);
        }
    }

    private async Task ApplyConsumerRescheduleAsync(
        IServiceProvider services,
        Guid eventId,
        IntegrationResultDisposition disposition,
        Result dispatchResult,
        CancellationToken cancellationToken)
    {
        var outboxStore = services.GetRequiredService<IIntegrationOutboxStore>();
        var line = await outboxStore.GetByEventIdAsync(eventId, cancellationToken);
        if (line is null)
        {
            return;
        }

        var maxAttempts = Math.Max(1, consumerOptions.Value.MaxAttempts);
        if (line.AttemptCount >= maxAttempts)
        {
            line.MarkDeadLetter(dispatchResult.Error.Code);
            MessagingConsumerMetrics.RecordDeadLetter(IntegrationsIntegrationMessageTypes.AdAccountSync);
            await outboxStore.UpdateAsync(line, cancellationToken);
            return;
        }

        var nextAttempt = disposition.NextAttemptAtUtc?.UtcDateTime ?? DateTime.UtcNow.AddMinutes(1);
        line.ScheduleForConsumerRetry(nextAttempt, dispatchResult.Error.Code, DateTime.UtcNow);
        await outboxStore.UpdateAsync(line, cancellationToken);
    }
}