using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Integrations.Application.Campaigns.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tenants.Application.Integrations.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.Services;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>Garante área dedicada à plataforma e conta escolhidas.</summary>
public sealed class AdPlatformManagementPageTests : BunitTestBase
{
    /// <summary>Uma página Google não lista campanhas de outra conta nem de outra rede.</summary>
    [Fact]
    public void Page_FiltersAccountsAndCampaignsByPlatformAndAccount()
    {
        var workspace = Guid.NewGuid(); var account = Guid.NewGuid();
        OAuthIntegrationsClientService.GetLinkedAdAccountsAsync(workspace, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ConnectedAdAccountDto>>.Success([
                new(account, workspace, "GoogleAds", "123", "Conta Google", "BRL", "Connected", false, DateTime.UtcNow),
                new(Guid.NewGuid(), workspace, "MetaAds", "act_11", "Conta Meta", "BRL", "Connected", false, DateTime.UtcNow)]));
        var campaigns = Substitute.For<IBulkCampaignClientService>();
        campaigns.GetCampaignsForBulkAsync(workspace, "GoogleAds", null, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CampaignHierarchyDto>>.Success([
                new(Guid.NewGuid(), workspace, account, "GoogleAds", "c1", "Campanha Google", "Active", "", null, null, "BRL", null, null, DateTime.UtcNow, []),
                new(Guid.NewGuid(), workspace, Guid.NewGuid(), "GoogleAds", "c2", "Outra conta", "Active", "", null, null, "BRL", null, null, DateTime.UtcNow, [])]));
        Services.AddSingleton(campaigns);
        var cut = Render<AdPlatformManagementPage>(p => p.Add(x => x.WorkspaceId, workspace).Add(x => x.Platform, "google"));
        cut.Markup.Should().Contain("Conta Google").And.Contain("Campanha Google").And.NotContain("Conta Meta").And.NotContain("Outra conta");
    }
}
