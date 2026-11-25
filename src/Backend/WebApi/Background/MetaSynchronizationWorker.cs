using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using Integrations.Application.Messaging;
using Integrations.Application.Persistence;
using Master.Application.Tenants.Queries.GetTenants;
using MediatR;
using Microsoft.Extensions.Options;
using Tenants.Application.Integrations.DTOs;
using Tenants.Application.Integrations.Queries.GetConnectedAdAccounts;
using Tenants.Application.Workspaces.Queries.GetWorkspaces;

namespace WebApi.Background;

/// <summary>Agendamento de leitura Meta usando a infraestrutura de BackgroundService já adotada.</summary>
public sealed class MetaSynchronizationOptions
{
    /// <summary>Habilita consultas externas periódicas; desabilitado por padrão.</summary>
    public bool Enabled { get; set; }
    /// <summary>Intervalo mínimo de cinco minutos entre ciclos completos.</summary>
    public int IntervalMinutes { get; set; } = 60;
    /// <summary>Dias recentes reprocessados para acomodar mudanças de atribuição.</summary>
    public int LookbackDays { get; set; } = 7;
}

/// <summary>Sincroniza contas reais vinculadas, em escopos novos por tenant/conta, sem mutações publicitárias.</summary>
/// <param name="scopeFactory">Fábrica de escopos independentes.</param>
/// <param name="options">Configurações validadas da operação.</param>
/// <param name="messagingFeatureGate">Feature flag de mensageria durável.</param>
/// <param name="logger">Logger estruturado sem credenciais.</param>
public sealed class MetaSynchronizationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MetaSynchronizationOptions> options,
    IMessagingFeatureGate messagingFeatureGate,
    ILogger<MetaSynchronizationWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (options.Value.Enabled)
            {
                try { await RunCycleAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception) { logger.LogError("Falha no ciclo Meta. Consulte a conectividade do catálogo e dos bancos operacionais."); }
            }
            try { await Task.Delay(TimeSpan.FromMinutes(Math.Max(5, options.Value.IntervalMinutes)), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    /// <summary>Processa somente tenants ativos/trial, workspaces ativos e vínculos Meta reais conectados.</summary>
    /// <param name="cancellationToken">Cancelamento do host.</param>
    /// <returns>Tarefa de execução sequencial sem sobreposição dentro desta instância.</returns>
    public async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var catalog = scopeFactory.CreateScope();
        var tenants = await catalog.ServiceProvider.GetRequiredService<ISender>().Send(new GetTenantsQuery(), cancellationToken);
        if (tenants.IsFailure) { logger.LogWarning("Catálogo Meta indisponível: {Code}", tenants.Error.Code); return; }
        foreach (var tenant in tenants.Value.Where(t => t.Status is "Active" or "Trial"))
        {
            using var tenantScope = scopeFactory.CreateScope();
            tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext =
                TenantContext.Create(tenant.Id, tenant.Subdomain, TenantResolutionSource.None);
            var sender = tenantScope.ServiceProvider.GetRequiredService<ISender>();
            var workspaces = await sender.Send(new GetWorkspacesQuery(true), cancellationToken);
            if (workspaces.IsFailure) continue;
            foreach (var workspace in workspaces.Value.Where(w => w.IsActive))
            {
                var accounts = await sender.Send(new GetConnectedAdAccountsQuery(workspace.Id), cancellationToken);
                if (accounts.IsFailure) continue;
                foreach (var account in accounts.Value.Where(a => a.WorkspaceId == workspace.Id && a.Platform == "MetaAds" && !a.IsDemo && a.Status == "Connected"))
                {
                    try { await RunAccountAsync(tenant.Id, account, cancellationToken); }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception) { logger.LogWarning("Falha de sincronização Meta: tenant {TenantId}, workspace {WorkspaceId}, conta {AccountId}", tenant.Id, account.WorkspaceId, account.Id); }
                }
            }
        }
    }

    /// <summary>Enfileira outbox ou executa sync in-process conforme mensageria.</summary>
    /// <param name="tenantId">Tenant já confirmado como elegível no catálogo.</param>
    /// <param name="account">Conta confirmada pelo módulo Tenants no workspace ativo.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Tarefa de sincronização de leitura ou gravação de outbox.</returns>
    public async Task RunAccountAsync(Guid tenantId, ConnectedAdAccountDto account, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || account.IsDemo || account.Platform != "MetaAds" || account.Status != "Connected") return;
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext = TenantContext.Create(tenantId, null, TenantResolutionSource.None);

        if (messagingFeatureGate.IsEnabled)
        {
            await EnqueueAccountSyncAsync(scope, tenantId, account, cancellationToken);
            return;
        }

        var executor = scope.ServiceProvider.GetRequiredService<IConnectedAdAccountMetaSyncExecutor>();
        await executor.ExecuteAsync(tenantId, account, cancellationToken);
    }

    private async Task EnqueueAccountSyncAsync(
        IServiceScope scope,
        Guid tenantId,
        ConnectedAdAccountDto account,
        CancellationToken cancellationToken)
    {
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IIntegrationOutboxWriter>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IIntegrationsUnitOfWork>();
        var occurredAtUtc = DateTime.UtcNow;
        var outboxResult = AdAccountSyncOutboxFactory.Create(
            tenantId,
            account.WorkspaceId,
            account.Id,
            account.Platform,
            occurredAtUtc);
        if (outboxResult.IsFailure)
        {
            logger.LogWarning(
                "Outbox Meta não criada: tenant {TenantId}, conta {AccountId}, {Code}",
                tenantId,
                account.Id,
                outboxResult.Error.Code);
            return;
        }

        var addResult = await outboxWriter.AddAsync(outboxResult.Value, cancellationToken);
        if (addResult.IsFailure)
        {
            logger.LogWarning(
                "Falha ao gravar outbox Meta: tenant {TenantId}, conta {AccountId}, {Code}",
                tenantId,
                account.Id,
                addResult.Error.Code);
            return;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Outbox Meta enfileirada: tenant {TenantId}, workspace {WorkspaceId}, conta {AccountId}, event {EventId}",
            tenantId,
            account.WorkspaceId,
            account.Id,
            outboxResult.Value.EventId);
    }
}