using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Status.DTOs;
using SocialPublishing.Domain.Posts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Controllers.v1;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para GET /api/v1/social/status (Subfase 1.4).
/// </summary>
public sealed class SocialStatusEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string JwtTestKey = "social-status-test-jwt-key-minimum-32-characters";

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SocialStatusEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Sem autenticação retorna 401 com envelope Result.
    /// </summary>
    [Fact]
    public async Task GetStatus_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/social/status?workspaceId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var envelope = await response.Content.ReadFromJsonAsync<Result<SocialModuleStatusDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsFailure.Should().BeTrue();
        envelope.Error.Code.Should().Be("Auth.Unauthorized");
    }

    /// <summary>
    /// Workspace inexistente retorna 404 Workspace.NotFound.
    /// </summary>
    [Fact]
    public async Task GetStatus_WhenWorkspaceNotFound_ShouldReturnNotFound()
    {
        var client = CreateClient(new FakeWorkspaceRepository(), allowAllPermissions: true);

        var response = await client.GetAsync($"/api/v1/social/status?workspaceId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = await response.Content.ReadFromJsonAsync<Result<SocialModuleStatusDto>>(JsonOptions);
        envelope!.Error.Code.Should().Be("Workspace.NotFound");
    }

    /// <summary>
    /// Workspace válido retorna 200 com contagens zeradas.
    /// </summary>
    [Fact]
    public async Task GetStatus_WhenWorkspaceExists_ShouldReturnOkWithZeroCounts()
    {
        var workspaceId = Guid.NewGuid();
        var repo = new FakeWorkspaceRepository();
        var workspace = Workspace.Create(workspaceId, "Cliente Social", "123.456.789-09", 1000m, "Varejo").Value;
        repo.Items.Add(workspace);

        var client = CreateClient(repo, allowAllPermissions: true);

        var response = await client.GetAsync($"/api/v1/social/status?workspaceId={workspaceId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<Result<SocialModuleStatusDto>>(JsonOptions);
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Connections.Should().Be(0);
        envelope.Value.ScheduledPosts.Should().Be(0);
    }

    /// <summary>
    /// Sem permissão SocialRead retorna 403.
    /// </summary>
    [Fact]
    public async Task GetStatus_WhenForbidden_ShouldReturnForbidden()
    {
        var workspaceId = Guid.NewGuid();
        var repo = new FakeWorkspaceRepository();
        repo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);

        var client = CreateClient(repo, allowAllPermissions: false);

        var response = await client.GetAsync($"/api/v1/social/status?workspaceId={workspaceId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var envelope = await response.Content.ReadFromJsonAsync<Result<SocialModuleStatusDto>>(JsonOptions);
        envelope!.Error.Code.Should().Be("Auth.Forbidden");
    }

    /// <summary>
    /// WorkspaceId vazio retorna 400 Social.WorkspaceRequired.
    /// </summary>
    [Fact]
    public async Task GetStatus_WhenWorkspaceEmpty_ShouldReturnBadRequest()
    {
        var client = CreateClient(new FakeWorkspaceRepository(), allowAllPermissions: true);

        var response = await client.GetAsync($"/api/v1/social/status?workspaceId={Guid.Empty}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await response.Content.ReadFromJsonAsync<Result<SocialModuleStatusDto>>(JsonOptions);
        envelope!.Error.Code.Should().Be("Social.WorkspaceRequired");
    }

    /// <summary>
    /// O método GET do SocialController declara EndpointSummary e ProducesResponseType 200/404.
    /// </summary>
    [Fact]
    public void GetStatusAction_ShouldDeclareOpenApiAttributes()
    {
        var method = typeof(SocialController).GetMethod(nameof(SocialController.GetStatus));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<EndpointSummaryAttribute>().Should().NotBeEmpty();
        var produces = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();
        produces.Should().Contain(p => p.StatusCode == StatusCodes.Status200OK);
        produces.Should().Contain(p => p.StatusCode == StatusCodes.Status404NotFound);
    }

    private HttpClient CreateClient(FakeWorkspaceRepository workspaceRepo, bool allowAllPermissions)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["TenantJwt:SecretKey"] = JwtTestKey }));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<Tenants.Application.Rbac.Services.IPermissionEvaluator>(_ =>
                    new TestPermissionEvaluator(allowAllPermissions));
                services.AddScoped<Tenants.Application.Squads.Services.IUserPortfolioService, TestPortfolio>();
                services.AddScoped<IWorkspaceRepository>(_ => workspaceRepo);
                services.AddScoped<IOrganicConnectionRepository, FakeOrganicConnectionRepository>();
                services.AddScoped<IOrganicPostRepository, FakeOrganicPostRepository>();
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

    private sealed class TestPermissionEvaluator(bool allow) : Tenants.Application.Rbac.Services.IPermissionEvaluator
    {
        public Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            TenantPermission permission,
            Tenants.Application.Rbac.Models.PermissionContext? context = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(allow && permission == TenantPermission.SocialRead));

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

    private sealed class FakeOrganicConnectionRepository : IOrganicConnectionRepository
    {
        public Task<SocialPublishing.Domain.Connections.OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SocialPublishing.Domain.Connections.OrganicConnection?>(null);

        public Task<SocialPublishing.Domain.Connections.OrganicConnection?> GetByWorkspacePlatformAndExternalAccountAsync(
            Guid workspaceId,
            SocialPublishing.Domain.Connections.OrganicPlatform platform,
            string externalAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SocialPublishing.Domain.Connections.OrganicConnection?>(null);

        public Task<IReadOnlyList<SocialPublishing.Domain.Connections.OrganicConnection>> ListByWorkspaceAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SocialPublishing.Domain.Connections.OrganicConnection>>(Array.Empty<SocialPublishing.Domain.Connections.OrganicConnection>());

        public Task AddAsync(SocialPublishing.Domain.Connections.OrganicConnection connection, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(SocialPublishing.Domain.Connections.OrganicConnection connection, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FakeOrganicPostRepository : IOrganicPostRepository
    {
        public Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganicPost?>(null);

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(Array.Empty<OrganicPost>());

        public Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAndStatusAsync(
            Guid workspaceId,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
