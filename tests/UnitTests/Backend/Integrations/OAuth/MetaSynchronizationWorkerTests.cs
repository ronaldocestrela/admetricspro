using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Persistence;
using Integrations.Domain.Meta;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tenants.Application.Integrations.DTOs;
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
        using var provider = services.BuildServiceProvider();
        var worker = new MetaSynchronizationWorker(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(new MetaSynchronizationOptions()), NullLogger<MetaSynchronizationWorker>.Instance);
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid();
        var workspace = Guid.NewGuid();
        var account = new ConnectedAdAccountDto(Guid.NewGuid(), workspace, "MetaAds", "act_11", "Conta", "BRL", "Connected", false, DateTime.UtcNow);
        await worker.RunAccountAsync(tenantA, account, default);
        await worker.RunAccountAsync(tenantB, account, default);
        contexts.Should().HaveCount(2);
        contexts[0].Should().NotBeSameAs(contexts[1]);
        contexts[0].TenantContext.TenantId.Should().Be(tenantA);
        contexts[1].TenantContext.TenantId.Should().Be(tenantB);
    }
}
