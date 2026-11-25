using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.Messaging.Scheduling;
using BuildingBlocks.Application.Messaging.Transport;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using Master.Application.FeatureFlags.Services;
using Master.Application.Tenants.Queries.GetTenants;
using Master.Domain.FeatureFlags;
using Master.Domain.Tenants;
using MediatR;
using Microsoft.Extensions.Options;
using WebApi.Configuration;

namespace WebApi.Background;

/// <summary>
/// Worker que reivindica linhas de outbox no Master e nos tenants e publica no broker.
/// </summary>
/// <param name="scopeFactory">Fábrica de escopos.</param>
/// <param name="messagingOptions">Feature flag global de mensageria.</param>
/// <param name="dispatchOptions">Opções do laço de despacho.</param>
/// <param name="logger">Logger estruturado.</param>
public sealed class OutboxDispatchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> messagingOptions,
    IOptions<OutboxDispatchOptions> dispatchOptions,
    ILogger<OutboxDispatchWorker> logger) : BackgroundService
{
    private static readonly System.Diagnostics.Metrics.Meter MessagingMeter = new("AdMetricsPro.Messaging");
    private static readonly System.Diagnostics.Metrics.Counter<long> DispatchPublished =
        MessagingMeter.CreateCounter<long>("admetrics.messaging.dispatch");

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!messagingOptions.Value.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = Math.Max(5, dispatchOptions.Value.IntervalSeconds);
            if (dispatchOptions.Value.Enabled)
            {
                try
                {
                    await RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha no ciclo de despacho de outbox.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Executa um ciclo completo de reclaim, lease, claim e publicação.
    /// </summary>
    /// <param name="cancellationToken">Cancelamento do host.</param>
    /// <returns>Tarefa do ciclo.</returns>
    public async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        if (!messagingOptions.Value.Enabled || !dispatchOptions.Value.Enabled)
        {
            logger.LogDebug("{Code}: despacho desabilitado.", MessagingErrors.Disabled.Code);
            return;
        }

        using var catalogScope = scopeFactory.CreateScope();
        var catalog = catalogScope.ServiceProvider;

        var featureFlags = catalog.GetRequiredService<IFeatureFlagService>();
        if (await featureFlags.IsFeatureEnabledAsync(MasterCatalogFeatureFlagKeys.MessagingDispatch, cancellationToken: cancellationToken))
        {
            logger.LogWarning("Kill switch {KillSwitchKey} armado; despacho de outbox ignorado.", MasterCatalogFeatureFlagKeys.MessagingDispatch);
            return;
        }

        var utcNow = DateTime.UtcNow;
        var dispatch = dispatchOptions.Value;

        var leader = catalog.GetRequiredService<IMessagingSchedulerLeader>();
        var leaseResult = await leader.TryLeadCycleAsync(cancellationToken);

        if (leaseResult.IsFailure)
        {
            logger.LogDebug("{Code}: lease não adquirido.", MessagingErrors.LeaseNotAcquired.Code);
            return;
        }

        var masterOutbox = catalog.GetRequiredService<MasterOutboxDispatchStore>();
        var publisher = catalog.GetRequiredService<IIntegrationEventPublisher>();

        await masterOutbox.ReclaimStaleAsync(utcNow, dispatch.InProgressTimeout, cancellationToken);
        await DrainStoreAsync(masterOutbox, publisher, utcNow, cancellationToken);

        var sender = catalog.GetRequiredService<ISender>();
        var tenants = await sender.Send(new GetTenantsQuery(), cancellationToken);
        if (tenants.IsFailure)
        {
            logger.LogWarning("Catálogo de tenants indisponível para despacho: {Code}", tenants.Error.Code);
            return;
        }

        foreach (var tenant in tenants.Value.Where(t =>
                     t.Status is nameof(TenantStatus.Active) or nameof(TenantStatus.Trial)))
        {
            try
            {
                await DrainTenantAsync(tenant.Id, tenant.Subdomain, publisher, utcNow, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha de despacho no tenant {TenantId}.", tenant.Id);
            }
        }
    }

    private async Task DrainTenantAsync(
        Guid tenantId,
        string subdomain,
        IIntegrationEventPublisher publisher,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        using var tenantScope = scopeFactory.CreateScope();
        tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext =
            TenantContext.Create(tenantId, subdomain, TenantResolutionSource.None);

        var tenantOutbox = tenantScope.ServiceProvider.GetRequiredService<IIntegrationOutboxStore>();
        await tenantOutbox.ReclaimStaleAsync(utcNow, dispatchOptions.Value.InProgressTimeout, cancellationToken);
        await DrainTenantStoreAsync(tenantOutbox, publisher, utcNow, cancellationToken);
    }

    private async Task DrainStoreAsync(
        MasterOutboxDispatchStore store,
        IIntegrationEventPublisher publisher,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await store.TryClaimNextAsync(utcNow, cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!await TryPublishLineAsync(store, publisher, line, utcNow, cancellationToken,
                    store.RequeueAsync, store.MarkPublishedAsync))
            {
                break;
            }
        }
    }

    private async Task DrainTenantStoreAsync(
        IIntegrationOutboxStore store,
        IIntegrationEventPublisher publisher,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await store.TryClaimNextAsync(utcNow, cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!await TryPublishLineAsync(store, publisher, line, utcNow, cancellationToken,
                    store.RequeueAsync, store.MarkPublishedAsync))
            {
                break;
            }
        }
    }

    private async Task<bool> TryPublishLineAsync(
        object storeToken,
        IIntegrationEventPublisher publisher,
        IntegrationOutboxMessage line,
        DateTime utcNow,
        CancellationToken cancellationToken,
        Func<IntegrationOutboxMessage, CancellationToken, Task> requeueAsync,
        Func<IntegrationOutboxMessage, CancellationToken, Task> markPublishedAsync)
    {
        _ = storeToken;

        var publishResult = await publisher.PublishAsync(line, cancellationToken);
        if (publishResult.IsFailure)
        {
            if (line.Requeue(utcNow.AddSeconds(30), publishResult.Error.Code, utcNow).IsSuccess)
            {
                await requeueAsync(line, cancellationToken);
            }
            logger.LogWarning("{Code}: falha ao publicar EventId {EventId}.", publishResult.Error.Code, line.EventId);
            return false;
        }

        if (line.MarkPublished().IsSuccess)
        {
            await markPublishedAsync(line, cancellationToken);
            DispatchPublished.Add(1, new KeyValuePair<string, object?>("message-type", line.MessageType));
        }
        return true;
    }
}