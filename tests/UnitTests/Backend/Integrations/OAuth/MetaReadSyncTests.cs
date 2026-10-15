using System.Globalization;
using System.Net;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Infrastructure.Campaigns.Mappers;
using Integrations.Infrastructure.Campaigns.Resilience;
using Integrations.Infrastructure.Campaigns.Sync;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Regressões de paginação e semântica das métricas reais.</summary>
public sealed class MetaReadSyncTests
{
    /// <summary>Não inventa fator monetário para moedas não cobertas pela conversão de orçamento.</summary>
    [Fact]
    public void HierarchyMapper_UnsupportedBudgetCurrency_PreservesUnknownBudget()
    {
        var result = new MetaAdsHierarchyMapper().Map("""{"data":[{"id":"11","name":"Cliente","daily_budget":"10000"}]}""", null, null, "JPY");
        result.Value.Campaigns.Single().DailyBudget.Should().BeNull();
    }
    /// <summary>Campanhas após a primeira página também precisam ser sincronizadas.</summary>
    [Fact]
    public async Task Hierarchy_ReadsAllCampaignPagesUsingConfiguredVersion()
    {
        var requests = new List<string>();
        var client = new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            var uri = request.RequestUri!.ToString();
            requests.Add(uri);
            request.RequestUri.AbsolutePath.Should().StartWith("/v26.0/");
            var json = uri.Contains("/campaigns?")
                ? uri.Contains("after=")
                    ? """{"data":[{"id":"22","name":"Segunda","status":"PAUSED","daily_budget":"12345"}]}"""
                    : """{"data":[{"id":"11","name":"Primeira","status":"ACTIVE"}],"paging":{"cursors":{"after":"cursor"},"next":"https://graph.facebook.com/next"}}"""
                : """{"data":[]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }));
        var adapter = new MetaAdsHierarchySyncAdapter(client, new MetaAdsHierarchyMapper(), new HierarchyRateLimitPolicy(),
            Options.Create(new MetaAdsOAuthOptions { GraphApiVersion = "v26.0", AppSecret = "secret" }));
        var account = ConnectedAdAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "MetaAds", "act_1", "Cliente").Value;
        var result = await adapter.FetchHierarchyAsync(account, "token");
        result.IsSuccess.Should().BeTrue();
        result.Value.Campaigns.Should().HaveCount(2);
        result.Value.Campaigns.Last().DailyBudget.Should().Be(123.45m);
        requests.Should().HaveCount(4);
    }

    /// <summary>Ações de clique e engajamento não equivalem a conversões ou compras.</summary>
    [Fact]
    public async Task Metrics_ParsesInvariantDecimalsWithoutSummingUnrelatedActions()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            var client = new HttpClient(new TestHttpMessageHandler((_, _) => new(HttpStatusCode.OK)
            { Content = new StringContent("""{"data":[{"campaign_id":"11","date_start":"2026-10-01","spend":"12.34","impressions":"100","clicks":"10","actions":[{"action_type":"link_click","value":"10"},{"action_type":"purchase","value":"2"}]}]}""") }));
            var adapter = new MetaAdsMetricsSyncAdapter(client, new HierarchyRateLimitPolicy(), Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
            var account = ConnectedAdAccount.Create(Guid.NewGuid(), Guid.NewGuid(), "MetaAds", "act_1", "Cliente").Value;
            var result = await adapter.FetchMetricsAsync(account, "token", new(2026, 10, 1), new(2026, 10, 2), MetricGranularity.Daily);
            result.IsSuccess.Should().BeTrue();
            result.Value.Single().Spend.Should().Be(12.34m);
            result.Value.Single().Conversions.Should().Be(0m, "nenhuma ação de conversão foi definida para esta conta");
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
