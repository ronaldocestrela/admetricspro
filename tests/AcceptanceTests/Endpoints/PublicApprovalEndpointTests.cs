using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Approvals;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Approvals;
using SocialPublishing.Domain.Posts;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Auth.Services;
using Tenants.Application.Branding.Repositories;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Aceitação das rotas públicas de aprovação orgânica (Subfase 5.3.3).
/// </summary>
public sealed class PublicApprovalEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateTime FixedUtc = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Inicializa a suíte.
    /// </summary>
    /// <param name="factory">Fábrica Web.</param>
    public PublicApprovalEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// GET anônimo com token válido retorna 200 e envelope Result.
    /// </summary>
    [Fact]
    public async Task GetApprovalBoard_WithValidToken_ShouldReturn200()
    {
        var workspaceId = Guid.NewGuid();
        var plainToken = ApprovalTokenHasher.GeneratePlainToken(out var bytes);
        var hash = ApprovalTokenHasher.HashBytes(bytes);
        var linkRepo = new AcceptanceApprovalLinkRepository();
        linkRepo.AllLinks.Add(ApprovalLink.Create(workspaceId, hash, FixedUtc.AddDays(3), FixedUtc).Value);

        var postRepo = new PublicApprovalPostRepository();
        var client = CreatePublicClient(linkRepo, postRepo);

        var response = await client.GetAsync($"/api/v1/public/social/approvals/{plainToken}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<Result<PublicApprovalBoardDto>>(JsonOptions);
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value!.AgencyName.Should().Be("Agência Teste");
    }

    /// <summary>
    /// Token inexistente retorna 404 com código estável.
    /// </summary>
    [Fact]
    public async Task GetApprovalBoard_WithInvalidToken_ShouldReturn404WithSameCode()
    {
        var client = CreatePublicClient(new AcceptanceApprovalLinkRepository(), new PublicApprovalPostRepository());

        var response = await client.GetAsync("/api/v1/public/social/approvals/invalid-token");
        var envelope = await response.Content.ReadFromJsonAsync<Result<PublicApprovalBoardDto>>(JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        envelope!.Error!.Code.Should().Be(SocialErrors.ApprovalTokenInvalid.Code);
    }

    /// <summary>
    /// Link expirado retorna o mesmo código que token inexistente.
    /// </summary>
    [Fact]
    public async Task GetApprovalBoard_WithExpiredLink_ShouldReturnSameErrorAsUnknown()
    {
        var workspaceId = Guid.NewGuid();
        var plainToken = ApprovalTokenHasher.GeneratePlainToken(out var bytes);
        var hash = ApprovalTokenHasher.HashBytes(bytes);
        var linkRepo = new AcceptanceApprovalLinkRepository();
        linkRepo.AllLinks.Add(ApprovalLink.Create(workspaceId, hash, FixedUtc.AddMinutes(-1), FixedUtc.AddDays(-1)).Value);

        var client = CreatePublicClient(linkRepo, new PublicApprovalPostRepository());

        var unknown = await client.GetAsync("/api/v1/public/social/approvals/unknown-token-value");
        var expired = await client.GetAsync($"/api/v1/public/social/approvals/{plainToken}");

        var unknownBody = await unknown.Content.ReadFromJsonAsync<Result<PublicApprovalBoardDto>>(JsonOptions);
        var expiredBody = await expired.Content.ReadFromJsonAsync<Result<PublicApprovalBoardDto>>(JsonOptions);

        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        expired.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknownBody!.Error!.Code.Should().Be(expiredBody!.Error!.Code);
    }

    /// <summary>
    /// POST decisions anônimo com token inválido retorna 404 normalizado.
    /// </summary>
    [Fact]
    public async Task PostDecision_WithInvalidToken_ShouldReturn404()
    {
        var client = CreatePublicClient(new AcceptanceApprovalLinkRepository(), new PublicApprovalPostRepository());

        var payload = new PublicApprovalDecisionRequest(
            new[] { Guid.NewGuid() },
            PublicApprovalDecision.Approve,
            null);

        var response = await client.PostAsJsonAsync(
            "/api/v1/public/social/approvals/invalid/decisions",
            payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreatePublicClient(
        AcceptanceApprovalLinkRepository linkRepo,
        PublicApprovalPostRepository postRepo)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IApprovalLinkRepository>(_ => linkRepo);
                services.AddScoped<IOrganicPostRepository>(_ => postRepo);
                services.AddScoped<ISocialPublishingUnitOfWork, PublicApprovalStubUnitOfWork>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(FixedUtc));
                services.AddScoped<ITenantBrandingRepository>(_ => new StubTenantBrandingRepository());
                services.AddScoped<ITenantAuthService>(_ => new StubTenantAuthService());
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", "test-agency");
        return client;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class PublicApprovalStubUnitOfWork : ISocialPublishingUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class StubTenantBrandingRepository : ITenantBrandingRepository
    {
        public Task<TenantBranding?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantBranding?>(TenantBranding.Create(
                Guid.NewGuid(),
                "#112233",
                "#445566",
                "https://logo.test/light.png",
                null,
                null).Value);

        public Task AddAsync(TenantBranding branding, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubTenantAuthService : ITenantAuthService
    {
        public Task<Result<AuthenticatedTenantUserDto>> AuthenticateAsync(
            string email,
            string password,
            string? subdomain = null,
            Guid? tenantId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<TenantPublicBrandingDto>> GetPublicBrandingAsync(
            string subdomain,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<TenantPublicBrandingDto>.Success(
                new TenantPublicBrandingDto(
                    Guid.NewGuid(),
                    "Agência Teste",
                    subdomain,
                    null,
                    "#112233",
                    "#445566",
                    "https://logo.test/light.png",
                    true)));
    }

    private sealed class AcceptanceApprovalLinkRepository : IApprovalLinkRepository
    {
        public List<ApprovalLink> AllLinks { get; } = new();

        public Task<ApprovalLink?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllLinks.FirstOrDefault(l => l.TokenHash == tokenHash));

        public Task AddAsync(ApprovalLink link, CancellationToken cancellationToken = default)
        {
            AllLinks.Add(link);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ApprovalLink link, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> RevokeActiveByWorkspaceAsync(
            Guid workspaceId,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class PublicApprovalPostRepository : IOrganicPostRepository
    {
        public Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganicPost?>(null);

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid ws, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(Array.Empty<OrganicPost>());

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceScheduledRangeAsync(
            Guid ws,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(Array.Empty<OrganicPost>());

        public Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAndStatusAsync(
            Guid ws,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAndStatusAsync(
            Guid ws,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(Array.Empty<OrganicPost>());
    }
}
