using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Domain.Connections;
using UnitTests.Frontend.Common;
using WebApp.Services.Social;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários do cliente HTTP <see cref="SocialPublishingClient"/>.
/// </summary>
public sealed class SocialPublishingClientTests
{
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly ITenantSessionStateProvider _sessionStateProvider = Substitute.For<ITenantSessionStateProvider>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Configura tenant mockado para os testes.
    /// </summary>
    public SocialPublishingClientTests()
    {
        _tenantStateProvider.CurrentTenant.Returns(new TenantState(_tenantId, "Tenant", "tenant", null, TenantBranding.Default));
    }

    /// <summary>
    /// ListConnectionsAsync envia X-Tenant-Id e usa rota versionada com envelope Result.
    /// </summary>
    [Fact]
    public async Task ListConnectionsAsync_ShouldSendTenantHeaderAndDeserializeResult()
    {
        var workspaceId = Guid.NewGuid();
        var dto = new OrganicConnectionSummaryDto(
            Guid.NewGuid(),
            workspaceId,
            OrganicPlatform.Instagram,
            "Marca",
            "ig-1",
            OrganicConnectionStatus.Active,
            DateTime.UtcNow.AddDays(3));

        string? capturedTenantHeader = null;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.PathAndQuery.Should().Be($"/api/v1/social/connections?workspaceId={workspaceId}");
            request.Headers.TryGetValues("X-Tenant-Id", out var values).Should().BeTrue();
            capturedTenantHeader = values!.First();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Success(new[] { dto }), JsonOptions))
            };
        });

        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);
        var result = await client.ListConnectionsAsync(workspaceId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        capturedTenantHeader.Should().Be(_tenantId.ToString());
    }

    /// <summary>
    /// StartAuthorizationAsync envia POST na rota authorize versionada.
    /// </summary>
    [Fact]
    public async Task StartAuthorizationAsync_ShouldPostVersionedAuthorizeRoute()
    {
        var workspaceId = Guid.NewGuid();
        var responsePayload = Result<StartOrganicAuthorizationResponse>.Success(
            new StartOrganicAuthorizationResponse("https://facebook.com/oauth", "state-token", DateTime.UtcNow.AddMinutes(15)));

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/social/connections/authorize");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responsePayload, JsonOptions))
            };
        });

        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);
        var result = await client.StartAuthorizationAsync(workspaceId, OrganicPlatform.TikTok, "https://app.test/social/connections");

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthorizationUrl.Should().Contain("facebook.com/oauth");
    }
}
