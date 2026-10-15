using System.Net;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Domain.Meta;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>CPA e ROAS só existem quando o evento e seus valores são explícitos.</summary>
public sealed class MetaInsightsTests
{
    /// <summary>Não inventa conversões; quando definido, usa somente o evento escolhido.</summary>
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("purchase", 5, 4)]
    public async Task ReadAsync_UsesOnlyExplicitAction(string? actionType, int? expectedCpa, int? expectedRoas)
    {
        var client = new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            var uri = Uri.UnescapeDataString(request.RequestUri!.Query);
            uri.Should().Contain("level=ad").And.Contain("time_increment=1");
            return new(HttpStatusCode.OK) { Content = new StringContent("""{"data":[{"date_start":"2026-10-01","date_stop":"2026-10-01","campaign_id":"11","adset_id":"22","ad_id":"33","spend":"10.00","clicks":"20","inline_link_clicks":"12","reach":"80","impressions":"100","actions":[{"action_type":"link_click","value":"12"},{"action_type":"purchase","value":"2"}],"action_values":[{"action_type":"purchase","value":"40"}]}]}""") };
        }));
        var api = new MetaInsightsApi(client, Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.ReadAsync("act_1", "token", new(new(2026, 10, 1), new(2026, 10, 1), "ad", ConversionAction: actionType), default);
        result.IsSuccess.Should().BeTrue();
        result.Value.Single().Cpa.Should().Be(expectedCpa.HasValue ? (decimal)expectedCpa.Value : null);
        result.Value.Single().Roas.Should().Be(expectedRoas.HasValue ? (decimal)expectedRoas.Value : null);
        result.Value.Single().LinkClicks.Should().Be(12);
        result.Value.Single().Clicks.Should().Be(20);
    }
}
