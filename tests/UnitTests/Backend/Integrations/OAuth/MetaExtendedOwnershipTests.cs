using System.Net;
using System.Text.Json;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Referências de cópia também precisam pertencer à conta vinculada.</summary>
public sealed class MetaExtendedOwnershipTests
{
    /// <summary>Não copia objetos de outra conta mesmo quando a credencial conhece as duas contas.</summary>
    /// <param name="resource">Recurso solicitado.</param>
    /// <param name="source">Campo de origem.</param>
    [Theory]
    [InlineData("campaigns", "source_campaign_id")]
    [InlineData("adsets", "source_adset_id")]
    [InlineData("ads", "source_ad_id")]
    public async Task Write_WithForeignSource_DoesNotPost(string resource, string source)
    {
        var posts = 0;
        var api = new MetaManagementApi(new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post) posts++;
            return new(HttpStatusCode.OK) { Content = new StringContent("""{"id":"123","account_id":"999"}""") };
        })), Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var payload = JsonSerializer.SerializeToElement(new Dictionary<string, object> { [source] = "123", ["name"] = "Cópia" });
        var result = await api.WriteAsync("act_11", resource, null, payload, "token", default);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Meta.ResourceAccountMismatch");
        posts.Should().Be(0);
    }
    /// <summary>A troca de conjunto não permite mover um anúncio para outra conta.</summary>
    [Fact]
    public async Task EditAd_WithForeignParent_DoesNotPost()
    {
        var posts = 0;
        var api = new MetaManagementApi(new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post) posts++;
            var own = request.RequestUri!.AbsolutePath.EndsWith("/123");
            return new(HttpStatusCode.OK) { Content = new StringContent(own
                ? """{"id":"123","account_id":"11"}""" : """{"id":"456","account_id":"999"}""") };
        })), Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.WriteAsync("act_11", "ads", "123", JsonSerializer.SerializeToElement(new { adset_id = "456" }), "token", default);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Meta.ResourceAccountMismatch");
        posts.Should().Be(0);
    }
}
