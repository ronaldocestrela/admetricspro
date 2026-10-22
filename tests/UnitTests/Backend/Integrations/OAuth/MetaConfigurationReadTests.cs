using System.Net;
using System.Text.Json;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Configurações não podem vazar entre contas e campos indisponíveis não apagam os demais.</summary>
public sealed class MetaConfigurationReadTests
{
    /// <summary>Confirma propriedade antes de consultar campos adicionais.</summary>
    [Fact]
    public async Task ReadConfiguration_WithForeignAccount_DoesNotReadExtraFields()
    {
        var requests = 0;
        var api = new MetaManagementApi(new HttpClient(new TestHttpMessageHandler((_, _) =>
        { requests++; return new(HttpStatusCode.OK) { Content = new StringContent("""{"id":"123","account_id":"999"}""") }; })),
            Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.ReadConfigurationAsync("act_11", "campaigns", "123", ["buying_type"], "token", default);
        result.Error.Code.Should().Be("Meta.ResourceAccountMismatch");
        requests.Should().Be(1);
    }

    /// <summary>Retorna valores reais e preserva zero/false sem inventar padrões na edição.</summary>
    [Fact]
    public async Task ReadConfiguration_ReturnsCurrentValues()
    {
        var api = new MetaManagementApi(new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            var extra = Uri.UnescapeDataString(request.RequestUri!.Query).Contains("buying_type");
            return new(HttpStatusCode.OK) { Content = new StringContent(extra
                ? """{"account_id":"11","buying_type":"AUCTION","is_adset_budget_sharing_enabled":false}"""
                : """{"id":"123","account_id":"11","name":"Campanha real","daily_budget":"12000"}""") };
        })), Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.ReadConfigurationAsync("act_11", "campaigns", "123", ["buying_type", "is_adset_budget_sharing_enabled"], "token", default);
        result.IsSuccess.Should().BeTrue();
        result.Value.Values.GetProperty("daily_budget").GetString().Should().Be("12000");
        result.Value.Values.GetProperty("is_adset_budget_sharing_enabled").GetBoolean().Should().BeFalse();
    }

    /// <summary>Campos arbitrários não se transformam em consultas Graph.</summary>
    [Fact]
    public async Task ReadConfiguration_WithUnknownField_DoesNotCallGraph()
    {
        var requests = 0;
        var api = new MetaManagementApi(new HttpClient(new TestHttpMessageHandler((_, _) =>
        { requests++; return new(HttpStatusCode.OK); })), Options.Create(new MetaAdsOAuthOptions()));
        (await api.ReadConfigurationAsync("act_11", "ads", "123", ["access_token"], "token", default)).IsFailure.Should().BeTrue();
        requests.Should().Be(0);
    }
}
