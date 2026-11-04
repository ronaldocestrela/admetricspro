using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using Master.Application.FeatureFlags.Services;
using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Application.Tenants.Queries.GetTenants;
using Master.Domain.FeatureFlags;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;
using SocialPublishing.Application.Traction.Commands.ScanOrganicTraction;
using WebApi.Background;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Publishing;

/// <summary>
/// Testes do worker de disparo orgânico (escopos, tenants e kill switch).
/// </summary>
public sealed class SocialPublishingDispatchWorkerTests
{
    /// <summary>
    /// Kill switch armado não envia comando de disparo.
    /// </summary>
    [Fact]
    public async Task RunCycleAsync_KillSwitchArmado_NaoDespachaComando()
    {
        var sender = Substitute.For<ISender>();
        var featureFlags = Substitute.For<IFeatureFlagService>();
        featureFlags.IsFeatureEnabledAsync(MasterCatalogFeatureFlagKeys.SocialPublishingDispatch, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var worker = CreateWorker(sender, featureFlags, null);
        await worker.RunCycleAsync(CancellationToken.None);

        await sender.DidNotReceive().Send(Arg.Any<GetTenantsQuery>(), Arg.Any<CancellationToken>());
        await sender.DidNotReceive().Send(Arg.Any<DispatchDuePublicationsCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Apenas tenants Active e Trial recebem disparo.
    /// </summary>
    [Fact]
    public async Task RunCycleAsync_SomenteTenantsActiveOuTrial()
    {
        var tenantActive = Guid.NewGuid();
        var tenantTrial = Guid.NewGuid();
        var tenantSuspended = Guid.NewGuid();

        var catalogSender = Substitute.For<ISender>();
        catalogSender.Send(Arg.Any<GetTenantsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantDetailsResponse>>.Success(
            [
                CreateTenant(tenantActive, "Active"),
                CreateTenant(tenantTrial, "Trial"),
                CreateTenant(tenantSuspended, "Suspended"),
            ]));

        var dispatchedTenants = new List<Guid>();
        var tenantServices = BuildTenantServices(dispatchedTenants);

        var featureFlags = Substitute.For<IFeatureFlagService>();
        featureFlags.IsFeatureEnabledAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);

        var worker = CreateWorker(catalogSender, featureFlags, tenantServices);
        await worker.RunCycleAsync(CancellationToken.None);

        dispatchedTenants.Should().BeEquivalentTo([tenantActive, tenantTrial]);
    }

    /// <summary>
    /// Contexto de tenant é definido antes do comando MediatR.
    /// </summary>
    [Fact]
    public async Task RunTenantAsync_DefineTenantContextAntesDoComando()
    {
        var tenantId = Guid.NewGuid();
        ITenantContextAccessor? captured = null;
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<DispatchDuePublicationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DispatchDuePublicationsSummary>.Success(new(0, 0, 0, 0)));

        var services = new ServiceCollection();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ISender>(_ =>
        {
            captured = _.GetRequiredService<ITenantContextAccessor>();
            return sender;
        });
        using var provider = services.BuildServiceProvider();

        var worker = new SocialPublishingDispatchWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new SocialPublishingDispatchOptions()),
            NullLogger<SocialPublishingDispatchWorker>.Instance);

        await worker.RunTenantAsync(tenantId, "acme", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantContext.TenantId.Should().Be(tenantId);
        await sender.Received(1).Send(Arg.Any<DispatchDuePublicationsCommand>(), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(Arg.Any<ScanOrganicTractionCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Kill switch armado não envia scan de tração.
    /// </summary>
    [Fact]
    public async Task RunCycleAsync_KillSwitchArmado_NaoEnviaScanDeTraction()
    {
        var sender = Substitute.For<ISender>();
        var featureFlags = Substitute.For<IFeatureFlagService>();
        featureFlags.IsFeatureEnabledAsync(MasterCatalogFeatureFlagKeys.SocialPublishingDispatch, null, Arg.Any<CancellationToken>())
            .Returns(true);

        var worker = CreateWorker(sender, featureFlags, null);
        await worker.RunCycleAsync(CancellationToken.None);

        await sender.DidNotReceive().Send(Arg.Any<ScanOrganicTractionCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Exceção de infraestrutura em um tenant não impede o próximo.
    /// </summary>
    [Fact]
    public async Task RunCycleAsync_ExcecaoEmUmTenant_ContinuaProximo()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var catalogSender = Substitute.For<ISender>();
        catalogSender.Send(Arg.Any<GetTenantsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantDetailsResponse>>.Success(
            [
                CreateTenant(tenantA, "Active"),
                CreateTenant(tenantB, "Active"),
            ]));

        var featureFlags = Substitute.For<IFeatureFlagService>();
        featureFlags.IsFeatureEnabledAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);

        var dispatched = new List<Guid>();
        var tenantServices = new ServiceCollection();
        tenantServices.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        tenantServices.AddScoped<ISender>(sp =>
        {
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            var tenantId = accessor.TenantContext.TenantId!.Value;
            dispatched.Add(tenantId);
            if (tenantId == tenantA)
            {
                throw new InvalidOperationException("infra");
            }

            return Substitute.For<ISender>();
        });
        using var tenantProvider = tenantServices.BuildServiceProvider();

        var worker = CreateWorker(catalogSender, featureFlags, tenantProvider);
        await worker.RunCycleAsync(CancellationToken.None);

        dispatched.Should().HaveCount(2);
    }

    private static ServiceProvider BuildTenantServices(List<Guid> dispatchedTenants)
    {
        var tenantServices = new ServiceCollection();
        tenantServices.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        tenantServices.AddScoped<ISender>(sp =>
        {
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            dispatchedTenants.Add(accessor.TenantContext.TenantId!.Value);
            var inner = Substitute.For<ISender>();
            inner.Send(Arg.Any<DispatchDuePublicationsCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result<DispatchDuePublicationsSummary>.Success(new(0, 0, 0, 0)));
            inner.Send(Arg.Any<ScanOrganicTractionCommand>(), Arg.Any<CancellationToken>())
                .Returns(Result<ScanOrganicTractionSummary>.Success(new(0, 0)));
            return inner;
        });
        return tenantServices.BuildServiceProvider();
    }

    private static SocialPublishingDispatchWorker CreateWorker(
        ISender catalogSender,
        IFeatureFlagService featureFlags,
        ServiceProvider? tenantProvider)
    {
        var scopeFactory = new WorkerTestScopeFactory(catalogSender, featureFlags, tenantProvider);
        return new SocialPublishingDispatchWorker(
            scopeFactory,
            Options.Create(new SocialPublishingDispatchOptions()),
            NullLogger<SocialPublishingDispatchWorker>.Instance);
    }

    private static TenantDetailsResponse CreateTenant(Guid id, string status) =>
        new(id, "Co", "00000000000000", "sub", status, "Pro", null, DateTime.UtcNow);

    private sealed class WorkerTestScopeFactory : IServiceScopeFactory
    {
        private readonly ISender _catalogSender;
        private readonly IFeatureFlagService _featureFlags;
        private readonly ServiceProvider? _tenantProvider;
        private int _scopeCount;

        public WorkerTestScopeFactory(ISender catalogSender, IFeatureFlagService featureFlags, ServiceProvider? tenantProvider)
        {
            _catalogSender = catalogSender;
            _featureFlags = featureFlags;
            _tenantProvider = tenantProvider;
        }

        public IServiceScope CreateScope()
        {
            if (Interlocked.Increment(ref _scopeCount) == 1)
            {
                var catalog = new ServiceCollection();
                catalog.AddScoped(_ => _catalogSender);
                catalog.AddScoped(_ => _featureFlags);
                return catalog.BuildServiceProvider().CreateScope();
            }

            if (_tenantProvider is null)
            {
                throw new InvalidOperationException("Tenant provider required.");
            }

            return _tenantProvider.CreateScope();
        }
    }
}
