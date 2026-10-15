using System.Net;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Verifica descoberta paginada e proteção das credenciais da Meta.</summary>
public sealed class MetaAdAccountsApiTests
{
    /// <summary>A paginação deve usar cursor e nunca seguir URLs externas fornecidas na resposta.</summary>
    [Fact]
    public async Task ListAsync_FollowsCursorWithoutSendingTokenToPagingUrl()
    {
        var requests = new List<string>();
        var client = new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            requests.Add(request.RequestUri!.ToString());
            request.Headers.Authorization!.Parameter.Should().Be("private-token");
            request.RequestUri.Host.Should().Be("graph.facebook.com");
            request.RequestUri.Query.Should().NotContain("private-token");
            request.RequestUri.Query.Should().Contain("appsecret_proof=");
            var json = requests.Count == 1
                ? """{"data":[{"id":"act_11","name":"A","currency":"BRL","timezone_name":"America/Sao_Paulo","account_status":1,"user_tasks":["ANALYZE"]}],"paging":{"cursors":{"after":"next-cursor"},"next":"https://attacker.example/leak"}}"""
                : """{"data":[{"id":"act_22","name":"B","currency":"USD","account_status":2}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }));
        var api = new MetaAdAccountsApi(client, Options.Create(new MetaAdsOAuthOptions
        { AppId = "app", AppSecret = "secret", GraphApiVersion = "v26.0" }));
        var result = await api.ListAsync("private-token", default);
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(x => x.Id).Should().Equal("act_11", "act_22");
        requests[1].Should().Contain("after=next-cursor");
    }

    /// <summary>Erros externos não devem devolver tokens ou o corpo bruto ao cliente.</summary>
    [Fact]
    public async Task ListAsync_InvalidToken_ReturnsSanitizedError()
    {
        var client = new HttpClient(new TestHttpMessageHandler((_, _) => new(HttpStatusCode.BadRequest)
        { Content = new StringContent("""{"error":{"code":190,"message":"private-token"}}""") }));
        var api = new MetaAdAccountsApi(client, Options.Create(new MetaAdsOAuthOptions { AppId = "app", AppSecret = "secret" }));
        var result = await api.ListAsync("private-token", default);
        result.Error.Code.Should().Be("Meta.TokenInvalid");
        result.Error.Description.Should().NotContain("private-token");
    }
}
