using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.Messaging.Scheduling;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Persistence;
using Integrations.Domain.Meta;
using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Application.Tenants.Queries.GetTenants;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tenants.Application.Integrations.DTOs;
using Tenants.Application.Integrations.Queries.GetConnectedAdAccounts;
using Tenants.Application.Workspaces.DTOs;
using Tenants.Application.Workspaces.Queries.GetWorkspaces;
using WebApi.Background;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Um job deve criar escopos distintos e resolver tenant antes das dependências.</summary>
public sealed class MetaSynchronizationWorkerTests
{
    /// <summary>Contas de tenants diferentes nunca compartilham contexto de banco.</summary>
    [Fact]
    public async Task RunAccountAsync_UsesIndependentTenantScopes()
    {
        var contexts = new List<ITenantContextAccessor>();
        var services = new ServiceCollection();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped(_ => Substitute.For<IIntegrationsUnitOfWork>());
        services.AddSingleton<IMessagingFeatureGate>(_ => new FixedMessagingFeatureGate(enabled: false));
        services.AddSingleton(Options.Create(new MetaSynchronizationOptions()));
        services.AddScoped(_ =>
        {
            var journal = Substitute.For<IMetaOperationRepository>();
            journal.TryBeginAsync(Arg.Any<BuildingBlocks.Domain.Integrations.MetaApiOperation>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));
            return journal;
        });
        services.AddScoped<ISender>(sp =>
        {
            var context = sp.GetRequiredService<ITenantContextAccessor>();
            contexts.Add(context);
            context.TenantContext.TenantId.Should().NotBeNull();
            var sender = Substitute.For<ISender>();
            sender.Send(Arg.Any<SyncCampaignHierarchyCommand>(), Arg.Any<CancellationToken>()).Returns(Result<SyncCampaignHierarchySummaryDto>.Success(new(1, 0, 0, 0, DateTime.UtcNow, new[] { "MetaAds" })));
            sender.Send(Arg.Any<SyncCampaignMetricsCommand>(), Arg.Any<CancellationToken>()).Returns(Result<SyncCampaignMetricsSummaryDto>.Success(new(1, 0, 0m, 0, 0, 0m, 0m, DateTime.UtcNow, "Daily", new[] { "MetaAds" })));
            return sender;
        });
        services.AddLogging();
        services.AddScoped<IConnectedAdAccountMetaSyncExecutor, ConnectedAdAccountMetaSyncExecutor>();
        using var provider = services.BuildServiceProvider();
        var worker = CreateWorker(provider, messagingEnabled: false);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var workspace = Guid.NewGuid();
        var account = new ConnectedAdAccountDto(Guid.NewGuid(), workspace, "MetaAds", "act_11", "Conta", "BRL", "Connected", false, DateTime.UtcNow);
        await worker.RunAccountAsync(tenantA, account, default);
        await worker.RunAccountAsync(tenantB, account, default);
        contexts.Should().HaveCount(2);
        contexts[0].Should().NotBeSameAs(contexts[1]);
        contexts[0].TenantContext.TenantId.Should().Be(tenantA);
        contexts[1].TenantContext.TenantId.Should().Be(tenantB);
    }

    /// <summary>Com mensageria ligada, o ciclo grava outbox por conta e não chama sync.</summary>
    [Fact]
    public async Task RunCycleAsync_WhenMessagingEnabled_EnqueuesOutboxWithoutSyncCommands()
    {
        var tenantActive = Guid.NewGuid();
        var tenantSuspended = Guid.NewGuid();
        var workspaceActive = Guid.NewGuid();
        var workspaceInactive = Guid.NewGuid();
        var account1 = new ConnectedAdAccountDto(Guid.NewGuid(), workspaceActive, "MetaAds", "act_1", "A1", "BRL", "Connected", false, DateTime.UtcNow);
        var account2 = new ConnectedAdAccountDto(Guid.NewGuid(), workspaceActive, "MetaAds", "act_2", "A2", "BRL", "Connected", false, DateTime.UtcNow);
        var accountDemo = new ConnectedAdAccountDto(Guid.NewGuid(), workspaceActive, "MetaAds", "act_demo", "Demo", "BRL", "Connected", true, DateTime.UtcNow);
        var accountDisconnected = new ConnectedAdAccountDto(Guid.NewGuid(), workspaceActive, "MetaAds", "act_off", "Off", "BRL", "Disconnected", false, DateTime.UtcNow);
        var accountInactiveWs = new ConnectedAdAccountDto(Guid.NewGuid(), workspaceInactive, "MetaAds", "act_3", "A3", "BRL", "Connected", false, DateTime.UtcNow);

        var outboxAdds = 0;
        var outboxWriter = Substitute.For<IIntegrationOutboxWriter>();
        outboxWriter.AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                outboxAdds++;
                return Result.Success();
            });

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetTenantsQuery>(), Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<TenantDetailsResponse>>.Success(
            [
                new(tenantActive, "A", "00000000000000", "a", "Active", "Pro", null, DateTime.UtcNow),
                new(tenantSuspended, "S", "00000000000001", "s", "Suspended", "Pro", null, DateTime.UtcNow)
            ]));

        sender.Send(Arg.Any<GetWorkspacesQuery>(), Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<WorkspaceDto>>.Success(
            [
                new(workspaceActive, "WS", "00000000000000", 1000m, null, true, DateTime.UtcNow, null),
                new(workspaceInactive, "WS2", "00000000000001", 500m, null, false, DateTime.UtcNow, null)
            ]));

        sender.Send(Arg.Any<GetConnectedAdAccountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.ArgAt<GetConnectedAdAccountsQuery>(0);
                if (query.WorkspaceId == workspaceActive)
                {
                    return Result<IReadOnlyList<ConnectedAdAccountDto>>.Success([account1, account2, accountDemo, accountDisconnected]);
                }

                if (query.WorkspaceId == workspaceInactive)
                {
                    return Result<IReadOnlyList<ConnectedAdAccountDto>>.Success([accountInactiveWs]);
                }

                return Result<IReadOnlyList<ConnectedAdAccountDto>>.Success(Array.Empty<ConnectedAdAccountDto>());
            });

        var leader = Substitute.For<IMessagingSchedulerLeader>();
        leader.TryLeadCycleAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        var services = new ServiceCollection();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped(_ => sender);
        services.AddScoped(_ => outboxWriter);
        services.AddScoped(_ => Substitute.For<IIntegrationsUnitOfWork>());
        services.AddScoped(_ => leader);
        services.AddSingleton(Options.Create(new MetaSynchronizationOptions()));
        services.AddSingleton<IMessagingFeatureGate>(_ => new FixedMessagingFeatureGate(enabled: true));
        services.AddScoped<IConnectedAdAccountMetaSyncExecutor>(_ => throw new InvalidOperationException("Sync não deve ser chamado no ciclo enfileirado."));
        using var provider = services.BuildServiceProvider();

        var worker = CreateWorker(provider, messagingEnabled: true);
        await worker.RunCycleAsync(default);

        outboxAdds.Should().Be(2);
        await sender.DidNotReceive().Send(Arg.Any<SyncCampaignHierarchyCommand>(), Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<SyncCampaignMetricsCommand>(), Arg.Any<CancellationToken>());
    }

    private static MetaSynchronizationWorker CreateWorker(ServiceProvider provider, bool messagingEnabled)
    {
        var gate = new FixedMessagingFeatureGate(messagingEnabled);
        return new MetaSynchronizationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MetaSynchronizationOptions { Enabled = true }),
            gate,
            NullLogger<MetaSynchronizationWorker>.Instance);
    }

    private sealed class FixedMessagingFeatureGate(bool enabled) : IMessagingFeatureGate
    {
        public bool IsEnabled { get; } = enabled;
    }
}