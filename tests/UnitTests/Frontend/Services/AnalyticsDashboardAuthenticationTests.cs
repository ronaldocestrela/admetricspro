using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Tenants.Application.Auth.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Models;
using WebApp.Services;
using WebApp.State;

namespace UnitTests.Frontend.Services;

/// <summary>Verifica as credenciais encaminhadas pelo dashboard à API protegida.</summary>
public sealed class AnalyticsDashboardAuthenticationTests
{
    /// <summary>Encaminha o token atual e acompanha a troca de sessão sem reter credenciais anteriores.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetExecutiveDashboardAsync_UsesCurrentSessionForEachRequest(bool campaigns)
    {
        var tenant = new TenantStateProvider();
        var session = new TenantSessionStateProvider(tenant);
        var tenantId = Guid.NewGuid();
        var headers = new List<(string? Scheme, string? Token, string? Tenant)>();
        using var handler = new TestHttpMessageHandler((request, _) =>
        {
            headers.Add((request.Headers.Authorization?.Scheme, request.Headers.Authorization?.Parameter,
                request.Headers.TryGetValues("X-Tenant-Id", out var values) ? values.Single() : null));
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = JsonContent.Create(Result.Failure(Error.Unauthorized("Auth.Unauthorized", "Sessão inválida.")))
            };
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new AnalyticsDashboardClientService(http, tenant, session);
        var campaignClient = new BulkCampaignClientService(http, tenant, session);
        foreach (var token in new[] { "first-session-token", "new-session-token" })
        {
            session.SetSession(new AuthenticatedTenantUserDto(token, "Bearer", 3600, Guid.NewGuid(),
                "owner@example.test", "Owner", "Owner", tenantId, "agency", null));
            Result result = campaigns ? await campaignClient.GetCampaignsForBulkAsync(Guid.NewGuid())
                : await client.GetExecutiveDashboardAsync(new DashboardFiltersState());
            result.Error.Code.Should().Be("Auth.Unauthorized");
        }
        session.ClearSession();
        if (campaigns) await campaignClient.GetCampaignsForBulkAsync(Guid.NewGuid());
        else await client.GetExecutiveDashboardAsync(new DashboardFiltersState());

        headers.Should().Equal(
            ("Bearer", "first-session-token", tenantId.ToString()),
            ("Bearer", "new-session-token", tenantId.ToString()),
            (null, null, null));
    }
}
