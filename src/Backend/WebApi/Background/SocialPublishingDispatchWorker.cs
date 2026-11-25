using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Scheduling;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using Master.Application.FeatureFlags.Services;
using Master.Application.Tenants.Queries.GetTenants;
using Master.Domain.FeatureFlags;
using MediatR;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;
using SocialPublishing.Application.Traction.Commands.ScanOrganicTraction;

namespace WebApi.Background;

/// <summary>
/// Worker que percorre tenants elegíveis e despacha publicações orgânicas devidas via MediatR.
/// </summary>
/// <param name="scopeFactory">Fábrica de escopos por tenant.</param>
/// <param name="options">Opções de agendamento.</param>
/// <param name="logger">Logger estruturado sem credenciais.</param>
public sealed class SocialPublishingDispatchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SocialPublishing.Application.Options.SocialPublishingDispatchOptions> options,
    ILogger<SocialPublishingDispatchWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (options.Value.Enabled)
            {
                try
                {
                    await RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    logger.LogError(
                        "Falha no ciclo de disparo orgânico. Verifique catálogo Master, feature flags e bancos de tenant.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, options.Value.IntervalSeconds)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Executa um ciclo completo respeitando kill switch e status de tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancelamento do host.</param>
    /// <returns>Tarefa de processamento sequencial por tenant.</returns>
    public async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var catalogScope = scopeFactory.CreateScope();
        var catalog = catalogScope.ServiceProvider;
        var messagingGate = catalog.GetRequiredService<IMessagingFeatureGate>();
        if (messagingGate.IsEnabled)
        {
            var lead = await catalog.GetRequiredService<IMessagingSchedulerLeader>().TryLeadCycleAsync(cancellationToken);
            if (lead.IsFailure)
            {
                logger.LogDebug("{Code}: ciclo orgânico ignorado.", MessagingErrors.LeaseNotAcquired.Code);
                return;
            }
        }

        var featureFlags = catalog.GetRequiredService<IFeatureFlagService>();
        if (await featureFlags.IsFeatureEnabledAsync(MasterCatalogFeatureFlagKeys.SocialPublishingDispatch, cancellationToken: cancellationToken))
        {
            logger.LogWarning("Kill switch {KillSwitchKey} armado; disparo orgânico ignorado neste ciclo.",
                MasterCatalogFeatureFlagKeys.SocialPublishingDispatch);
            return;
        }

        var sender = catalog.GetRequiredService<ISender>();
        var tenants = await sender.Send(new GetTenantsQuery(), cancellationToken);
        if (tenants.IsFailure)
        {
            logger.LogWarning("Catálogo de tenants indisponível para disparo orgânico: {Code}", tenants.Error.Code);
            return;
        }

        foreach (var tenant in tenants.Value.Where(t => t.Status is "Active" or "Trial"))
        {
            try
            {
                await RunTenantAsync(tenant.Id, tenant.Subdomain, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Falha de disparo orgânico no tenant {TenantId}; continuando com o próximo.",
                    tenant.Id);
            }
        }
    }

    /// <summary>
    /// Despacha publicações devidas em um tenant com contexto isolado.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="subdomain">Subdomínio do tenant para contexto.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Tarefa de execução do comando de disparo.</returns>
    public async Task RunTenantAsync(Guid tenantId, string subdomain, CancellationToken cancellationToken)
    {
        using var tenantScope = scopeFactory.CreateScope();
        tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext =
            TenantContext.Create(tenantId, subdomain, TenantResolutionSource.None);
        var services = tenantScope.ServiceProvider;
        var reclaimer = services.GetRequiredService<IOrganicPublicationClaimReclaimer>();
        await reclaimer.ReclaimStaleClaimsAsync(cancellationToken);

        var messagingGate = services.GetRequiredService<IMessagingFeatureGate>();
        if (messagingGate.IsEnabled)
        {
            var forwarder = services.GetRequiredService<IOrganicSlotForwarder>();
            await forwarder.ForwardAllDueAsync(cancellationToken);
        }
        else
        {
            var sender = services.GetRequiredService<ISender>();
            await sender.Send(new DispatchDuePublicationsCommand(), cancellationToken);
        }

        var tractionSender = services.GetRequiredService<ISender>();
        await tractionSender.Send(new ScanOrganicTractionCommand(), cancellationToken);
    }
}
