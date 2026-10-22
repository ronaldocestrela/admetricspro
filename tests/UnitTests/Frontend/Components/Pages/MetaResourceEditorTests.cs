using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Shared.Components;
using WebApp.Services;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>Configurações completas carregam valores remotos e enviam somente alterações explícitas.</summary>
public sealed class MetaResourceEditorTests : BunitTestBase
{
    /// <summary>Edição de orçamento preserva todas as demais configurações da campanha.</summary>
    [Fact]
    public async Task EditCampaign_LoadsRemoteValuesAndSendsOnlySelectedField()
    {
        var api = Configure("campaigns", """{"name":"Campanha real","daily_budget":"12000","buying_type":"AUCTION","objective":"OUTCOME_SALES","is_adset_budget_sharing_enabled":false}""");
        WriteMetaResourceCommand? submitted = null;
        api.WriteAsync(Arg.Do<WriteMetaResourceCommand>(r => submitted = r), Arg.Any<CancellationToken>())
            .Returns(Result<MetaWriteResult>.Success(new("123", true)));
        var cut = Render<MetaResourceEditor>(p => p.Add(x => x.WorkspaceId, Guid.NewGuid()).Add(x => x.AccountId, Guid.NewGuid())
            .Add(x => x.Resource, "campaigns").Add(x => x.ExternalId, "123").Add(x => x.Currency, "BRL"));
        cut.WaitForAssertion(() => cut.Find("[data-field='daily_budget']").GetAttribute("value").Should().Be("120.00"));
        cut.Find("[data-include='daily_budget']").Change(true);
        cut.Find("[data-field='daily_budget']").Change("150.25");
        cut.Find("[data-testid='meta-save-resource']").Click();
        cut.WaitForAssertion(() => submitted.Should().NotBeNull());
        submitted!.Data.EnumerateObject().Select(p => p.Name).Should().Equal("daily_budget");
        submitted.Data.GetProperty("daily_budget").GetInt64().Should().Be(15025);
        await api.Received(1).GetResourceConfigurationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), "campaigns", "123", null, Arg.Any<CancellationToken>());
    }

    /// <summary>Campos somente de criação são explicados em vez de reaplicados na edição.</summary>
    [Fact]
    public void EditCampaign_DoesNotAllowChangingBuyingType()
    {
        Configure("campaigns", """{"name":"Real","buying_type":"AUCTION"}""");
        var cut = Render<MetaResourceEditor>(p => p.Add(x => x.Resource, "campaigns").Add(x => x.ExternalId, "123"));
        cut.WaitForAssertion(() => cut.Find("[data-field-state='buying_type']").TextContent.Should().Contain("Somente na criação"));
        cut.FindAll("[data-include='buying_type']").Should().BeEmpty();
    }

    /// <summary>Falha de leitura não permite substituir configurações remotas por valores vazios.</summary>
    [Fact]
    public void EditAdSet_WhenReadFails_DoesNotOfferSave()
    {
        var api = Configure("adsets", "{}");
        api.GetResourceConfigurationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), "adsets", "123", null, Arg.Any<CancellationToken>())
            .Returns(Result<MetaResourceConfiguration>.Failure(Error.Forbidden("Meta.PermissionDenied", "Sem permissão para ler.")));
        var cut = Render<MetaResourceEditor>(p => p.Add(x => x.Resource, "adsets").Add(x => x.ExternalId, "123"));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Sem permissão para ler."));
        cut.FindAll("[data-testid='meta-save-resource']").Should().BeEmpty();
    }

    /// <summary>Alterar segmentação conserva localidades e públicos não removidos pelo usuário.</summary>
    [Fact]
    public void EditAdSet_ChangesAgeAndPreservesOtherTargeting()
    {
        var api = Configure("adsets", """{"name":"Real","targeting":{"geo_locations":{"countries":["BR"]},"age_min":21,"custom_audiences":[{"id":"998"}]}}""");
        WriteMetaResourceCommand? submitted = null;
        api.WriteAsync(Arg.Do<WriteMetaResourceCommand>(r => submitted = r), Arg.Any<CancellationToken>())
            .Returns(Result<MetaWriteResult>.Success(new("123", true)));
        var cut = Render<MetaResourceEditor>(p => p.Add(x => x.Resource, "adsets").Add(x => x.ExternalId, "123"));
        cut.WaitForAssertion(() => cut.Find("[data-include='targeting']").Should().NotBeNull());
        cut.Find("[data-include='targeting']").Change(true);
        cut.FindAll("[data-field-state='targeting'] button").First(b => b.TextContent == "Configurar campos").Click();
        cut.Find("[data-field='targeting.age_min']").Change("25");
        cut.Find("[data-testid='meta-save-resource']").Click();
        cut.WaitForAssertion(() => submitted.Should().NotBeNull());
        submitted!.Data.GetProperty("targeting").GetProperty("age_min").GetInt32().Should().Be(25);
        submitted.Data.GetProperty("targeting").GetProperty("custom_audiences")[0].GetProperty("id").GetString().Should().Be("998");
        submitted.Data.GetProperty("targeting").GetProperty("geo_locations").GetProperty("countries")[0].GetString().Should().Be("BR");
        submitted.Data.EnumerateObject().Should().ContainSingle();
    }

    /// <summary>Ativação de um anúncio existente exige confirmação antes do envio.</summary>
    [Fact]
    public void EditAd_ActivationRequiresConfirmation()
    {
        var api = Configure("ads", """{"name":"Real","status":"PAUSED","creative":{"id":"987"}}""");
        WriteMetaResourceCommand? submitted = null;
        api.WriteAsync(Arg.Do<WriteMetaResourceCommand>(r => submitted = r), Arg.Any<CancellationToken>())
            .Returns(Result<MetaWriteResult>.Success(new("123", true)));
        var cut = Render<MetaResourceEditor>(p => p.Add(x => x.Resource, "ads").Add(x => x.ExternalId, "123"));
        cut.WaitForAssertion(() => cut.Find("[data-include='status']").Should().NotBeNull());
        cut.Find("[data-include='status']").Change(true);
        cut.Find("[data-field='status']").Change("ACTIVE");
        cut.Find("[data-testid='meta-save-resource']").HasAttribute("disabled").Should().BeTrue();
        submitted.Should().BeNull();
        cut.Find(".meta-confirmation input").Change(true);
        cut.Find("[data-testid='meta-save-resource']").Click();
        cut.WaitForAssertion(() => submitted.Should().NotBeNull());
        submitted!.ConfirmActivation.Should().BeTrue();
        submitted.Data.GetProperty("status").GetString().Should().Be("ACTIVE");
        submitted.Data.EnumerateObject().Should().ContainSingle();
    }

    /// <summary>JSON incompleto não envia silenciosamente o valor anterior do campo.</summary>
    [Fact]
    public void EditAd_InvalidTrackingJsonBlocksSave()
    {
        Configure("ads", """{"name":"Real","tracking_specs":[]}""");
        var cut = Render<MetaResourceEditor>(p => p.Add(x => x.Resource, "ads").Add(x => x.ExternalId, "123"));
        cut.WaitForAssertion(() => cut.Find("[data-include='tracking_specs']").Should().NotBeNull());
        cut.Find("[data-include='tracking_specs']").Change(true);
        cut.FindAll("[data-field-state='tracking_specs'] button").First(b => b.TextContent == "Especificação avançada").Click();
        cut.Find("[data-json-field='tracking_specs']").Change("[{invalid");
        cut.Find("[data-testid='meta-save-resource']").HasAttribute("disabled").Should().BeTrue();
        cut.Find("[role='alert']").TextContent.Should().Contain("Revise");
    }

    private IMetaAdsClientService Configure(string resource, string json)
    {
        var api = Substitute.For<IMetaAdsClientService>();
        api.GetResourceOptionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), resource, Arg.Any<CancellationToken>())
            .Returns(MetaResourceCatalog.Get(resource));
        api.GetResourceConfigurationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), resource, Arg.Any<string>(), null, Arg.Any<CancellationToken>())
            .Returns(Result<MetaResourceConfiguration>.Success(new(JsonSerializer.Deserialize<JsonElement>(json), new Dictionary<string, string>())));
        Services.AddSingleton(api);
        return api;
    }
}
