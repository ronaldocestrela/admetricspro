using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Infrastructure.MultiTenancy;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;
using Integrations.Application.Persistence;
using Integrations.Domain.Meta;
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
/// <param name="logger">Logger estruturado sem credenciais.</param>
public sealed class MetaSynchronizationWorker(IServiceScopeFactory scopeFactory, IOptions<MetaSynchronizationOptions> options,
    ILogger<MetaSynchronizationWorker> logger) : BackgroundService
{
    private static readonly Guid SystemActorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly System.Diagnostics.Metrics.Meter Meter = new("AdMetricsPro.MetaSynchronization");
    private static readonly System.Diagnostics.Metrics.Counter<long> Runs = Meter.CreateCounter<long>("admetrics.meta.sync.runs");

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

    /// <summary>Cria o contexto antes de resolver mediador/repositórios e registra a execução sem segredos.</summary>
    /// <param name="tenantId">Tenant já confirmado como elegível no catálogo.</param>
    /// <param name="account">Conta confirmada pelo módulo Tenants no workspace ativo.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Tarefa de sincronização de leitura.</returns>
    public async Task RunAccountAsync(Guid tenantId, ConnectedAdAccountDto account, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || account.IsDemo || account.Platform != "MetaAds" || account.Status != "Connected") return;
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext = TenantContext.Create(tenantId, null, TenantResolutionSource.None);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var journal = scope.ServiceProvider.GetRequiredService<IMetaOperationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IIntegrationsUnitOfWork>();
        var operationId = Guid.NewGuid();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{tenantId}:{account.WorkspaceId}:{account.Id}:{operationId}")));
        var entry = MetaApiOperation.Create(operationId, account.WorkspaceId, account.Id, SystemActorId, "sync", hash).Value;
        var claim = await journal.TryBeginAsync(entry, cancellationToken);
        if (claim.IsFailure || !claim.Value) return;
        var hierarchy = await sender.Send(new SyncCampaignHierarchyCommand(account.WorkspaceId, account.Id, "MetaAds"), cancellationToken);
        string? failure = hierarchy.IsFailure ? hierarchy.Error.Code : null;
        if (failure is null)
        {
            // Intervalo cobre dias de conta em fusos distintos e reprocessa atribuição recente.
            var timeZone = TimeZoneInfo.Utc;
            if (account.TimeZone is { } name && TimeZoneInfo.TryFindSystemTimeZoneById(name, out var knownTimeZone)) timeZone = knownTimeZone;
            var until = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date, DateTimeKind.Utc);
            var metrics = await sender.Send(new SyncCampaignMetricsCommand(account.WorkspaceId, account.Id, "MetaAds",
                until.AddDays(-Math.Clamp(options.Value.LookbackDays, 1, 90)), until, MetricGranularity.Daily), cancellationToken);
            if (metrics.IsFailure) failure = metrics.Error.Code;
        }
        entry.Complete(account.ExternalAccountId, failure);
        journal.Update(entry);
        await unitOfWork.CommitAsync(cancellationToken);
        Runs.Add(1, new KeyValuePair<string, object?>("state", entry.State));
        logger.LogInformation("Sincronização Meta {OperationId}: tenant {TenantId}, workspace {WorkspaceId}, conta {AccountId}, estado {State}",
            operationId, tenantId, account.WorkspaceId, account.Id, entry.State);
    }
}
