using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tenants.Application.Integrations.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>O gerenciamento exige uma conta real vinculada ao workspace.</summary>
public sealed class MetaManagementPageTests : BunitTestBase
{
    /// <summary>Sem vínculo real não deve apresentar formulários que poderiam enviar publicidade.</summary>
    [Fact]
    public void Page_WithoutLinkedAccount_ExplainsConnectionBeforeManagement()
    {
        OAuthIntegrationsClientService.GetLinkedAdAccountsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ConnectedAdAccountDto>>.Success(Array.Empty<ConnectedAdAccountDto>()));
        Services.AddSingleton(Substitute.For<IMetaAdsClientService>());
        Services.AddSingleton(Substitute.For<IBulkCampaignClientService>());
        var cut = Render<MetaManagementPage>(p => p.Add(x => x.WorkspaceId, Guid.NewGuid()));
        cut.Markup.Should().Contain("Conecte a Meta e vincule uma conta de anúncios");
        cut.FindAll("[data-testid='meta-create-campaign']").Should().BeEmpty();
    }

    /// <summary>O formulário inicial envia uma campanha pausada ao backend.</summary>
    [Fact]
    public async Task Page_CreateCampaign_UsesPausedStatusWithoutActivationConfirmation()
    {
        var workspace = Guid.NewGuid(); var accountId = Guid.NewGuid();
        OAuthIntegrationsClientService.GetLinkedAdAccountsAsync(workspace, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ConnectedAdAccountDto>>.Success(new[] { new ConnectedAdAccountDto(accountId, workspace, "MetaAds", "act_11", "Cliente", "BRL", "Connected", false, DateTime.UtcNow) }));
        var campaigns = Substitute.For<IBulkCampaignClientService>();
        campaigns.GetCampaignsForBulkAsync(workspace, "MetaAds", null, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<global::Integrations.Application.Campaigns.DTOs.CampaignHierarchyDto>>.Success(Array.Empty<global::Integrations.Application.Campaigns.DTOs.CampaignHierarchyDto>()));
        Services.AddSingleton(campaigns);
        var meta = Substitute.For<IMetaAdsClientService>();
        meta.GetResourceOptionsAsync(workspace, accountId, "campaigns", Arg.Any<CancellationToken>())
            .Returns(global::Integrations.Application.Meta.MetaResourceCatalog.Get("campaigns"));
        meta.WriteAsync(Arg.Any<global::Integrations.Application.Meta.WriteMetaResourceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<global::Integrations.Domain.Meta.MetaWriteResult>.Success(new("123", true)));
        Services.AddSingleton(meta);
        var cut = Render<MetaManagementPage>(p => p.Add(x => x.WorkspaceId, workspace));
        cut.Find("[data-testid='meta-create-campaign']").Click();
        cut.Find("input[maxlength='200']").Change("Campanha de teste");
        cut.Find("[data-testid='meta-save-resource']").Click();
        await meta.Received(1).WriteAsync(Arg.Is<global::Integrations.Application.Meta.WriteMetaResourceCommand>(r =>
            r.WorkspaceId == workspace && r.AccountId == accountId && !r.ConfirmActivation &&
            r.Data.GetProperty("status").GetString() == "PAUSED"), Arg.Any<CancellationToken>());
        await meta.Received(1).WriteAsync(Arg.Is<global::Integrations.Application.Meta.WriteMetaResourceCommand>(r =>
            r.Data.GetProperty("is_adset_budget_sharing_enabled").ValueKind == System.Text.Json.JsonValueKind.False), Arg.Any<CancellationToken>());
    }
}
