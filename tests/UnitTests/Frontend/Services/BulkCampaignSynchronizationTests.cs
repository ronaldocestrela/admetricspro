using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Campaigns.DTOs;
using Tenants.Application.Auth.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Services;
using WebApp.State;

namespace UnitTests.Frontend.Services;

/// <summary>Verifica a sequência de sincronização pela API com conta, plataforma e sessão explícitas.</summary>
public sealed class BulkCampaignSynchronizationTests
{
    /// <summary>Sincroniza hierarquia e métricas da conta escolhida com JWT em ambas as chamadas.</summary>
    [Fact]
    public async Task SynchronizeAccountAsync_SendsAuthenticatedHierarchyThenMetrics()
    {
        var tenant = new TenantStateProvider();
        var session = new TenantSessionStateProvider(tenant);
        var workspace = Guid.NewGuid(); var account = Guid.NewGuid();
        session.SetSession(new("token", "Bearer", 3600, Guid.NewGuid(), "owner@test.example", "Owner", "Owner", Guid.NewGuid(), "agency", null));
        var paths = new List<string>();
        using var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Headers.Authorization!.Parameter.Should().Be("token");
            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            body.Should().Contain(workspace.ToString()).And.Contain(account.ToString()).And.Contain("GoogleAds");
            paths.Add(request.RequestUri!.AbsolutePath);
            return new(HttpStatusCode.OK) { Content = paths.Count == 1
                ? JsonContent.Create(Result<SyncCampaignHierarchySummaryDto>.Success(new(1, 1, 0, 0, DateTime.UtcNow, ["GoogleAds"])))
                : JsonContent.Create(Result<SyncCampaignMetricsSummaryDto>.Success(new(1, 1, 10, 100, 2, 0, 0, DateTime.UtcNow, "Daily", ["GoogleAds"]))) };
        });
        using var http = new HttpClient(handler) { BaseAddress = new("http://localhost:5000") };
        var client = new BulkCampaignClientService(http, tenant, session);
        var result = await client.SynchronizeAccountAsync(workspace, account, "GoogleAds", new(2026, 10, 1), new(2026, 10, 22));
        result.IsSuccess.Should().BeTrue();
        paths.Should().Equal("/api/v1/integrations/campaigns/sync", "/api/v1/integrations/campaigns/metrics/sync");
    }
}
