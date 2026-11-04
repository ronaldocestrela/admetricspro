using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Traction;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Domain.Connections;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Controllers.v1;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para rotas OAuth orgânico em /api/v1/social/connections (Subfase 2.2).
/// </summary>
public sealed class SocialConnectionsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string JwtTestKey = "social-connections-test-jwt-key-minimum-32-chars";

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SocialConnectionsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Sem autenticação retorna 401.
    /// </summary>
    [Fact]
    public async Task Authorize_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/social/connections/authorize",
            new { workspaceId = Guid.NewGuid(), platform = OrganicPlatform.Instagram, redirectUri = "https://cb" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Sem permissão SocialConnectionsManage retorna 403.
    /// </summary>
    [Fact]
    public async Task Authorize_WhenForbidden_ShouldReturnForbidden()
    {
        var workspaceId = Guid.NewGuid();
        var repo = new FakeWorkspaceRepository();
        repo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);

        var client = CreateClient(repo, allowConnectionsManage: false);
        var response = await client.PostAsJsonAsync(
            "/api/v1/social/connections/authorize",
            new { workspaceId, platform = OrganicPlatform.Instagram, redirectUri = "https://app/cb" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Workspace inexistente retorna 404.
    /// </summary>
    [Fact]
    public async Task Callback_WhenWorkspaceNotFound_ShouldReturnNotFound()
    {
        var client = CreateClient(new FakeWorkspaceRepository(), allowConnectionsManage: true);
        var response = await client.PostAsJsonAsync(
            "/api/v1/social/connections/callback",
            new
            {
                workspaceId = Guid.NewGuid(),
                code = "code",
                state = "state",
                redirectUri = "https://app/cb",
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Actions authorize e callback declaram atributos OpenAPI exigidos.
    /// </summary>
    [Fact]
    public void ConnectionActions_ShouldDeclareOpenApiAttributes()
    {
        AssertOpenApiAttributes(nameof(SocialController.AuthorizeOrganicConnection));
        AssertOpenApiAttributes(nameof(SocialController.CompleteOrganicAuthorization));
    }

    private static void AssertOpenApiAttributes(string actionName)
    {
        var method = typeof(SocialController).GetMethod(actionName);
        method.Should().NotBeNull();
        method!.GetCustomAttributes<EndpointSummaryAttribute>().Should().NotBeEmpty();
        var produces = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();
        produces.Should().Contain(p => p.StatusCode == StatusCodes.Status200OK);
        produces.Should().Contain(p => p.StatusCode == StatusCodes.Status400BadRequest);
        produces.Should().Contain(p => p.StatusCode == StatusCodes.Status422UnprocessableEntity);
    }

    private HttpClient CreateClient(FakeWorkspaceRepository workspaceRepo, bool allowConnectionsManage)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["TenantJwt:SecretKey"] = JwtTestKey }));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<Tenants.Application.Rbac.Services.IPermissionEvaluator>(_ =>
                    new TestPermissionEvaluator(allowConnectionsManage));
                services.AddScoped<Tenants.Application.Squads.Services.IUserPortfolioService, TestPortfolio>();
                services.AddScoped<IWorkspaceRepository>(_ => workspaceRepo);
                services.AddScoped<IOrganicConnectionRepository, StubOrganicConnectionRepository>();
                services.AddScoped<IOrganicPostRepository, StubOrganicPostRepository>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());
        return client;
    }

    private static string CreateJwt() => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer: "AdMetricsPro.Tenants",
        audience: "AdMetricsPro.ClientApp",
        claims:
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("tenant_id", TestTenantId.ToString())
        ],
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTestKey)),
            SecurityAlgorithms.HmacSha256)));

    private sealed class TestPermissionEvaluator(bool allowConnectionsManage)
        : Tenants.Application.Rbac.Services.IPermissionEvaluator
    {
        public Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            TenantPermission permission,
            Tenants.Application.Rbac.Models.PermissionContext? context = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(
                allowConnectionsManage && permission == TenantPermission.SocialConnectionsManage));

        public IReadOnlySet<TenantPermission> GetPermissionsForRole(TenantRole role) =>
            new HashSet<TenantPermission>();

        public Task<Result<IReadOnlySet<TenantPermission>>> GetPermissionsForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlySet<TenantPermission>>.Success(new HashSet<TenantPermission>()));
    }

    private sealed class TestPortfolio : Tenants.Application.Squads.Services.IUserPortfolioService
    {
        public Task<Result<bool>> HasAccessToWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public Task<Result<IReadOnlyList<Guid>>> GetAccessibleWorkspaceIdsAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<Guid>>.Success(Array.Empty<Guid>()));
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public List<Workspace> Items { get; } = new();

        public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Workspace>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Workspace>>(Items);

        public Task<bool> ExistsByCnpjOrCpfAsync(string cnpjOrCpf, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> CountActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(w => w.IsActive));

        public Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            Items.Add(workspace);
            return Task.CompletedTask;
        }

        public void Update(Workspace workspace)
        {
        }
    }

    private sealed class StubOrganicConnectionRepository : IOrganicConnectionRepository
    {
        public Task<OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganicConnection?>(null);

        public Task<OrganicConnection?> GetByWorkspacePlatformAndExternalAccountAsync(
            Guid workspaceId,
            OrganicPlatform platform,
            string externalAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganicConnection?>(null);

        public Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicConnection>>(Array.Empty<OrganicConnection>());

        public Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class StubOrganicPostRepository : IOrganicPostRepository
    {
        public Task<SocialPublishing.Domain.Posts.OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SocialPublishing.Domain.Posts.OrganicPost?>(null);

        public Task<IReadOnlyList<SocialPublishing.Domain.Posts.OrganicPost>> ListByWorkspaceAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SocialPublishing.Domain.Posts.OrganicPost>>(Array.Empty<SocialPublishing.Domain.Posts.OrganicPost>());

        public Task<IReadOnlyList<SocialPublishing.Domain.Posts.OrganicPost>> ListByWorkspaceScheduledRangeAsync(
            Guid workspaceId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SocialPublishing.Domain.Posts.OrganicPost>>(Array.Empty<SocialPublishing.Domain.Posts.OrganicPost>());

        public Task AddAsync(SocialPublishing.Domain.Posts.OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(SocialPublishing.Domain.Posts.OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAndStatusAsync(
            Guid workspaceId,
            SocialPublishing.Domain.Posts.OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<SocialPublishing.Domain.Posts.OrganicPost>> ListByWorkspaceAndStatusAsync(
            Guid workspaceId,
            SocialPublishing.Domain.Posts.OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SocialPublishing.Domain.Posts.OrganicPost>>(Array.Empty<SocialPublishing.Domain.Posts.OrganicPost>());

        public Task<IReadOnlyList<OrganicPostTractionCandidate>> ListPublishedForTractionAsync(
            DateTime publishedSinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPostTractionCandidate>>(Array.Empty<OrganicPostTractionCandidate>());
    }
}
