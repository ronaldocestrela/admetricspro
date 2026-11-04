using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using System.Globalization;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
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

    /// <summary>
    /// ListPostsAsync envia X-Tenant-Id e consulta GET versionada com intervalo UTC.
    /// </summary>
    [Fact]
    public async Task ListPostsAsync_ShouldSendTenantHeaderAndVersionedQuery()
    {
        var workspaceId = Guid.NewGuid();
        var fromUtc = new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 12, 1, 2, 59, 59, DateTimeKind.Utc);
        var dto = new OrganicPostDto(
            Guid.NewGuid(),
            workspaceId,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Legenda",
            fromUtc.AddDays(1),
            OrganicPostStatus.Scheduled);

        string? capturedTenantHeader = null;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.PathAndQuery.Should().Contain($"/api/v1/social/posts?workspaceId={workspaceId}");
            request.RequestUri.PathAndQuery.Should().Contain($"from={Uri.EscapeDataString(fromUtc.ToString("O", CultureInfo.InvariantCulture))}");
            request.RequestUri.PathAndQuery.Should().Contain($"to={Uri.EscapeDataString(toUtc.ToString("O", CultureInfo.InvariantCulture))}");
            request.Headers.TryGetValues("X-Tenant-Id", out var values).Should().BeTrue();
            capturedTenantHeader = values!.First();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(Result<IReadOnlyList<OrganicPostDto>>.Success(new[] { dto }), JsonOptions))
            };
        });

        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);
        var result = await client.ListPostsAsync(workspaceId, fromUtc, toUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        capturedTenantHeader.Should().Be(_tenantId.ToString());
    }

    /// <summary>
    /// RescheduleAsync envia POST na rota :reschedule com corpo scheduledAtUtc.
    /// </summary>
    [Fact]
    public async Task RescheduleAsync_ShouldPostVersionedRescheduleRoute()
    {
        var workspaceId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var scheduledAtUtc = new DateTime(2026, 11, 10, 13, 0, 0, DateTimeKind.Utc);
        var dto = new OrganicPostDto(
            postId,
            workspaceId,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Sale,
            "Legenda",
            scheduledAtUtc,
            OrganicPostStatus.Scheduled);

        string? requestBody = null;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.Should().Be($"/api/v1/social/posts/{postId}:reschedule?workspaceId={workspaceId}");
            requestBody = request.Content is null ? null : request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(Result<OrganicPostDto>.Success(dto), JsonOptions))
            };
        });

        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);
        var result = await client.RescheduleAsync(workspaceId, postId, scheduledAtUtc);

        result.IsSuccess.Should().BeTrue();
        requestBody.Should().NotBeNull();
        requestBody.Should().Contain("scheduledAtUtc");
        requestBody.Should().Contain("2026-11-10T13:00:00");
    }

    /// <summary>
    /// GetBrandVoiceAsync envia X-Tenant-Id e rota versionada.
    /// </summary>
    [Fact]
    public async Task GetBrandVoiceAsync_ShouldSendTenantHeaderAndVersionedRoute()
    {
        var workspaceId = Guid.NewGuid();
        var dto = new BrandVoiceDto(
            Guid.NewGuid(),
            workspaceId,
            "Tom",
            "Persona",
            Array.Empty<string>(),
            Array.Empty<string>(),
            EmojiPolicy.Sparingly,
            20,
            40,
            20,
            25,
            15,
            "pt-BR",
            DateTime.UtcNow);

        string? capturedTenantHeader = null;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/social/brand-voice/{workspaceId}");
            request.Headers.TryGetValues("X-Tenant-Id", out var values).Should().BeTrue();
            capturedTenantHeader = values!.First();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(Result<BrandVoiceDto>.Success(dto), JsonOptions)),
            };
        });

        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);
        var result = await client.GetBrandVoiceAsync(workspaceId);

        result.IsSuccess.Should().BeTrue();
        capturedTenantHeader.Should().Be(_tenantId.ToString());
    }

    /// <summary>
    /// UpsertBrandVoiceAsync envia PUT versionado com corpo JSON.
    /// </summary>
    [Fact]
    public async Task UpsertBrandVoiceAsync_ShouldPutVersionedRouteWithBody()
    {
        var workspaceId = Guid.NewGuid();
        var dto = new BrandVoiceDto(
            Guid.NewGuid(),
            workspaceId,
            "Tom",
            "Persona",
            new[] { "https://example.com" },
            new[] { "grátis" },
            EmojiPolicy.Forbidden,
            15,
            25,
            25,
            25,
            25,
            "pt-BR",
            DateTime.UtcNow);

        string? requestBody = null;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Put);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/social/brand-voice/{workspaceId}");
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(Result<BrandVoiceDto>.Success(dto), JsonOptions)),
            };
        });

        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);
        var result = await client.UpsertBrandVoiceAsync(
            workspaceId,
            "Tom",
            "Persona",
            new[] { "https://example.com" },
            new[] { "grátis" },
            EmojiPolicy.Forbidden,
            15,
            25,
            25,
            25,
            25,
            "pt-BR");

        result.IsSuccess.Should().BeTrue();
        requestBody.Should().Contain("authorityWeight");
        requestBody.Should().Contain("forbiddenTerms");
    }

    /// <summary>
    /// GeneratePlanAsync envia POST em plans:generate com envelope Result.
    /// </summary>
    [Fact]
    public async Task GeneratePlanAsync_ShouldPostVersionedGenerateRoute()
    {
        var workspaceId = Guid.NewGuid();
        var rangeStart = new DateOnly(2026, 11, 2);
        var rangeEnd = new DateOnly(2026, 11, 6);
        var plan = new GeneratedPlanDto(Array.Empty<Guid>(), Array.Empty<FailedEditorialSlotDto>());

        string? requestBody = null;
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/social/plans:generate");
            request.Headers.TryGetValues("X-Tenant-Id", out var tenantValues).Should().BeTrue();
            tenantValues.Should().NotBeNull();
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(Result<GeneratedPlanDto>.Success(plan), JsonOptions)),
            };
        });

        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);
        var result = await client.GeneratePlanAsync(workspaceId, rangeStart, rangeEnd, "America/Sao_Paulo", 5, 0);

        result.IsSuccess.Should().BeTrue();
        requestBody.Should().Contain("postsPerWeek");
        requestBody.Should().Contain("timeZoneId");
    }

    /// <summary>
    /// Falha de rede em GeneratePlanAsync retorna Social.NetworkError.
    /// </summary>
    [Fact]
    public async Task GeneratePlanAsync_WhenNetworkFails_ShouldReturnNetworkError()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("timeout"));
        var client = new SocialPublishingClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test") }, _tenantStateProvider, _sessionStateProvider);

        var result = await client.GeneratePlanAsync(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow), "America/Sao_Paulo", 3, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Social.NetworkError");
    }
}
