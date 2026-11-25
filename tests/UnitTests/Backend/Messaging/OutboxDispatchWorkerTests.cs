using BuildingBlocks.Application.Messaging.Persistence;
using FluentAssertions;
using Master.Application.FeatureFlags.Services;
using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Application.Tenants.Queries.GetTenants;
using Master.Domain.FeatureFlags;
using Master.Domain.Tenants;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using WebApi.Background;
using WebApi.Configuration;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes do <see cref="OutboxDispatchWorker"/>.
/// </summary>
public sealed class OutboxDispatchWorkerTests
{
    /// <summary>
    /// Com <c>Messaging:Enabled=false</c> o worker não executa o laço.
    /// </summary>
    [Fact]
    public async Task RunCycleAsync_MessagingDesligado_NaoConsultaTenants()
    {
        var sender = Substitute.For<ISender>();
        var worker = CreateWorker(
            sender,
            Substitute.For<IFeatureFlagService>(),
            messagingEnabled: false,
            dispatchEnabled: true);

        await worker.RunCycleAsync(CancellationToken.None);
        await sender.DidNotReceive().Send(Arg.Any<GetTenantsQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Kill switch de despacho impede consulta ao catálogo.
    /// </summary>
    [Fact]
    public async Task RunCycleAsync_KillSwitchArmado_NaoConsultaTenants()
    {
        var sender = Substitute.For<ISender>();
        var featureFlags = Substitute.For<IFeatureFlagService>();
        featureFlags
            .IsFeatureEnabledAsync(MasterCatalogFeatureFlagKeys.MessagingDispatch, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var worker = CreateWorker(sender, featureFlags, messagingEnabled: true, dispatchEnabled: true);
        await worker.RunCycleAsync(CancellationToken.None);

        await sender.DidNotReceive().Send(Arg.Any<GetTenantsQuery>(), Arg.Any<CancellationToken>());
    }

    private static OutboxDispatchWorker CreateWorker(
        ISender sender,
        IFeatureFlagService featureFlags,
        bool messagingEnabled,
        bool dispatchEnabled)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sender);
        services.AddSingleton(featureFlags);
        services.AddSingleton(Options.Create(new MessagingOptions { Enabled = messagingEnabled }));
        services.AddSingleton(Options.Create(new OutboxDispatchOptions { Enabled = dispatchEnabled, IntervalSeconds = 5 }));
        var provider = services.BuildServiceProvider();

        return new OutboxDispatchWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<MessagingOptions>>(),
            provider.GetRequiredService<IOptions<OutboxDispatchOptions>>(),
            NullLogger<OutboxDispatchWorker>.Instance);
    }
}