using System.Net;
using System.Text.Json;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Operações de escrita não podem atingir outra conta nem ser repetidas pelo transporte.</summary>
public sealed class MetaManagementApiTests
{
    /// <summary>Busca de targeting aceita somente tipos oficiais e codifica o texto de pesquisa.</summary>
    [Fact]
    public async Task SearchTargeting_UsesOfficialSearchAndEncodesQuery()
    {
        var api = new MetaManagementApi(new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            request.RequestUri!.AbsolutePath.Should().EndWith("/search");
            request.RequestUri.Query.Should().Contain("type=adinterest").And.Contain("q=marketing%20%26%20vendas");
            return new(HttpStatusCode.OK) { Content = new StringContent("""{"data":[{"id":"123","name":"Marketing"}]}""") };
        })), Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.SearchTargetingAsync("adinterest", "marketing & vendas", "token", default);
        result.IsSuccess.Should().BeTrue();
        (await api.SearchTargetingAsync("unsupported", "q", "token", default)).IsFailure.Should().BeTrue();
    }
    /// <summary>Antes de editar, confirma a conta proprietária do objeto externo.</summary>
    [Fact]
    public async Task WriteAsync_ResourceOwnedByAnotherAccount_DoesNotPost()
    {
        var posts = 0;
        var client = new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post) posts++;
            return new(HttpStatusCode.OK) { Content = new StringContent("""{"id":"123","account_id":"999"}""") };
        }));
        var api = new MetaManagementApi(client, Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.WriteAsync("act_11", "campaigns", "123", JsonSerializer.SerializeToElement(new { name = "Novo nome" }), "token", default);
        result.IsFailure.Should().BeTrue();
        posts.Should().Be(0);
    }

    /// <summary>Timeout de criação exige reconciliação; repetir POST pode duplicar a campanha.</summary>
    [Fact]
    public async Task WriteAsync_CreateTimeout_DoesNotRetry()
    {
        var posts = 0;
        var client = new HttpClient(new TestHttpMessageHandler((Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>)((request, _) =>
        {
            posts++;
            throw new HttpRequestException("private-token must never be returned");
        })));
        var api = new MetaManagementApi(client, Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.WriteAsync("act_11", "campaigns", null,
            JsonSerializer.SerializeToElement(new { name = "Teste", objective = "OUTCOME_SALES", status = "PAUSED", special_ad_categories = Array.Empty<string>() }), "private-token", default);
        posts.Should().Be(1);
        result.Error.Code.Should().Be("Meta.WriteOutcomeUnknown");
        result.Error.Description.Should().NotContain("private-token");
    }
}
