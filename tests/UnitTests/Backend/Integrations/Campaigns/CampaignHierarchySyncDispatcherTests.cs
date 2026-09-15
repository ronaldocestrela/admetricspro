using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Infrastructure.Campaigns.Sync;
using NSubstitute;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o despachante de sincronização hierárquica (CampaignHierarchySyncDispatcher).
/// </summary>
public sealed class CampaignHierarchySyncDispatcherTests
{
    private readonly ICampaignHierarchySyncAdapter _metaAdapter = Substitute.For<ICampaignHierarchySyncAdapter>();
    private readonly ICampaignHierarchySyncAdapter _demoAdapter = Substitute.For<ICampaignHierarchySyncAdapter>();

    /// <summary>
    /// Valida que contas em modo demonstração são despachadas exclusivamente para o adaptador Demo.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_WhenAccountIsDemo_ShouldRouteToDemoAdapter()
    {
        // Arrange
        _demoAdapter.Platform.Returns("Demo");
        _demoAdapter.FetchHierarchyAsync(
                Arg.Any<ConnectedAdAccount>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<UnifiedCampaignHierarchy>.Success(
                new UnifiedCampaignHierarchy(
                    new List<UnifiedCampaignItem>(),
                    new List<UnifiedAdSetItem>(),
                    new List<UnifiedAdItem>()))));

        var dispatcher = new CampaignHierarchySyncDispatcher(new[] { _demoAdapter, _metaAdapter });
        var demoAccount = ConnectedAdAccount.CreateDemo(Guid.NewGuid(), "MetaAds").Value;

        // Act
        var result = await dispatcher.DispatchAsync(demoAccount, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _demoAdapter.Received(1).FetchHierarchyAsync(demoAccount, null, Arg.Any<CancellationToken>());
        await _metaAdapter.DidNotReceive().FetchHierarchyAsync(Arg.Any<ConnectedAdAccount>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que contas reais autenticadas são despachadas para o adaptador de sua respectiva plataforma.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_WhenRealAccount_ShouldRouteToPlatformAdapter()
    {
        // Arrange
        _metaAdapter.Platform.Returns("MetaAds");
        _metaAdapter.FetchHierarchyAsync(
                Arg.Any<ConnectedAdAccount>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<UnifiedCampaignHierarchy>.Success(
                new UnifiedCampaignHierarchy(
                    new List<UnifiedCampaignItem>(),
                    new List<UnifiedAdSetItem>(),
                    new List<UnifiedAdItem>()))));

        var dispatcher = new CampaignHierarchySyncDispatcher(new[] { _demoAdapter, _metaAdapter });
        var realAccount = ConnectedAdAccount.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MetaAds",
            "act_123456789",
            "Conta Real Meta",
            "BRL",
            isDemo: false).Value;

        // Act
        var result = await dispatcher.DispatchAsync(realAccount, "valid_access_token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _metaAdapter.Received(1).FetchHierarchyAsync(realAccount, "valid_access_token", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que uma falha amigável é retornada quando não existe adaptador registrado para a plataforma solicitada.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_WhenUnknownPlatform_ShouldReturnFailure()
    {
        // Arrange
        var dispatcher = new CampaignHierarchySyncDispatcher(Array.Empty<ICampaignHierarchySyncAdapter>());
        var account = ConnectedAdAccount.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MetaAds",
            "act_999",
            "Conta Sem Adaptador",
            "BRL",
            isDemo: false).Value;

        // Act
        var result = await dispatcher.DispatchAsync(account, "token");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sync.AdapterNotFound");
    }
}
