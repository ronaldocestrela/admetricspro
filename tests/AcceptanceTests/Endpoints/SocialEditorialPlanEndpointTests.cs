using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
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
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Application.Planning.Copy;
using SocialPublishing.Domain.Planning;
using SocialPublishing.Domain.Posts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Controllers.v1;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para materialização de plano editorial (Subfase 3.4).
/// </summary>
public sealed class SocialEditorialPlanEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string JwtTestKey = "social-editorial-plan-test-jwt-key-min-32";

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SocialEditorialPlanEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Sem Brand Voice retorna 404 no envelope Result.
    /// </summary>
    [Fact]
    public async Task GeneratePlan_WithoutBrandVoice_ShouldReturn404()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId);

        var response = await client.PostAsync(
            "/api/v1/social/plans:generate",
            BuildJsonBody(ValidPlanRequest(workspaceId)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = JsonSerializer.Deserialize<Result<GeneratedPlanDto>>(await response.Content.ReadAsStringAsync(), JsonOptions);
        envelope!.Error!.Code.Should().Be(SocialErrors.BrandVoiceNotFound.Code);
    }

    /// <summary>
    /// Intervalo inválido retorna 422.
    /// </summary>
    [Fact]
    public async Task GeneratePlan_WithInvalidRange_ShouldReturn422()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId, seedBrandVoice: true);
        var body = ValidPlanRequest(workspaceId) with
        {
            RangeStart = new DateOnly(2026, 11, 10),
            RangeEnd = new DateOnly(2026, 11, 1)
        };

        var response = await client.PostAsync("/api/v1/social/plans:generate", BuildJsonBody(body));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var envelope = JsonSerializer.Deserialize<Result<GeneratedPlanDto>>(await response.Content.ReadAsStringAsync(), JsonOptions);
        envelope!.Error!.Code.Should().Be(SocialErrors.PlanRangeInvalid.Code);
    }

    /// <summary>
    /// GET de posts com intervalo vazio retorna 200.
    /// </summary>
    [Fact]
    public async Task ListPosts_WithEmptyRange_ShouldReturn200()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId);
        var from = Uri.EscapeDataString("2026-11-01T00:00:00Z");
        var to = Uri.EscapeDataString("2026-11-30T23:59:59Z");

        var response = await client.GetAsync($"/api/v1/social/posts?workspaceId={workspaceId}&from={from}&to={to}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = JsonSerializer.Deserialize<Result<IReadOnlyList<OrganicPostDto>>>(
            await response.Content.ReadAsStringAsync(),
            JsonOptions);
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().BeEmpty();
    }

    /// <summary>
    /// Endpoints expõem atributos OpenAPI exigidos.
    /// </summary>
    [Fact]
    public void EditorialPlanEndpoints_ShouldDeclareOpenApiAttributes()
    {
        AssertOpenApi(typeof(SocialController), nameof(SocialController.GenerateEditorialPlan));
        AssertOpenApi(typeof(SocialController), nameof(SocialController.ListOrganicPosts));
    }

    private static void AssertOpenApi(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        method.Should().NotBeNull();
        method!.GetCustomAttributes<EndpointSummaryAttribute>().Should().NotBeEmpty();
        var statusCodes = method.GetCustomAttributes<ProducesResponseTypeAttribute>().Select(a => a.StatusCode).ToList();
        statusCodes.Should().Contain(StatusCodes.Status200OK);
        statusCodes.Should().Contain(StatusCodes.Status400BadRequest);
        statusCodes.Should().Contain(StatusCodes.Status401Unauthorized);
        statusCodes.Should().Contain(StatusCodes.Status403Forbidden);
    }

    private static Guid SeedWorkspace() => Guid.NewGuid();

    private HttpClient CreateClient(Guid workspaceId, bool seedBrandVoice = false)
    {
        var workspaceRepo = new FakeWorkspaceRepository();
        workspaceRepo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);

        var brandVoiceRepo = new AcceptanceInMemoryBrandVoiceRepository();
        if (seedBrandVoice)
        {
            var profile = BrandVoiceProfile.Create(
                workspaceId,
                "Tom",
                "Persona",
                ["https://example.com"],
                [],
                EmojiPolicy.Sparingly,
                15,
                40,
                20,
                25,
                15).Value;
            brandVoiceRepo.AddAsync(profile).GetAwaiter().GetResult();
        }

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["TenantJwt:SecretKey"] = JwtTestKey }));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<Tenants.Application.Rbac.Services.IPermissionEvaluator>(_ =>
                    new TestPermissionEvaluator());
                services.AddScoped<Tenants.Application.Squads.Services.IUserPortfolioService, TestPortfolio>();
                services.AddScoped<IWorkspaceRepository>(_ => workspaceRepo);
                services.AddSingleton(brandVoiceRepo);
                services.AddScoped<IBrandVoiceRepository>(sp => sp.GetRequiredService<AcceptanceInMemoryBrandVoiceRepository>());
                services.AddScoped<IOrganicPostRepository, StubOrganicPostRepository>();
                services.AddScoped<IOrganicConnectionRepository, StubConnectionRepository>();
                services.AddScoped<IEditorialPlanGenerationRepository, StubPlanGenerationRepository>();
                services.AddScoped<ISocialPublishingUnitOfWork, AcceptanceStubSocialUnitOfWork>();
                services.AddScoped<ISocialCreativeGenerator, StubCreativeGenerator>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt());
        return client;
    }

    private static GenerateEditorialPlanRequest ValidPlanRequest(Guid workspaceId) =>
        new(
            workspaceId,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            "America/Sao_Paulo",
            5,
            0);

    private static StringContent BuildJsonBody(GenerateEditorialPlanRequest request) =>
        new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

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

    private sealed class TestPermissionEvaluator : Tenants.Application.Rbac.Services.IPermissionEvaluator
    {
        public Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            TenantPermission permission,
            Tenants.Application.Rbac.Models.PermissionContext? context = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public IReadOnlySet<TenantPermission> GetPermissionsForRole(TenantRole role) => new HashSet<TenantPermission>();

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

    private sealed class AcceptanceInMemoryBrandVoiceRepository : IBrandVoiceRepository
    {
        private readonly Dictionary<Guid, BrandVoiceProfile> _byWorkspace = new();

        public Task<BrandVoiceProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byWorkspace.Values.FirstOrDefault(p => p.Id == id));

        public Task<BrandVoiceProfile?> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byWorkspace.TryGetValue(workspaceId, out var profile) ? profile : null);

        public Task<IReadOnlyList<BrandVoiceProfile>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BrandVoiceProfile>>(
                _byWorkspace.TryGetValue(workspaceId, out var profile) ? [profile] : Array.Empty<BrandVoiceProfile>());

        public Task AddAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default)
        {
            _byWorkspace[profile.WorkspaceId] = profile;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default)
        {
            _byWorkspace[profile.WorkspaceId] = profile;
            return Task.CompletedTask;
        }
    }

    private sealed class AcceptanceStubSocialUnitOfWork : ISocialPublishingUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class StubOrganicPostRepository : IOrganicPostRepository
    {
        public Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganicPost?>(null);

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(Array.Empty<OrganicPost>());

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceScheduledRangeAsync(
            Guid workspaceId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(Array.Empty<OrganicPost>());

        public Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAndStatusAsync(
            Guid workspaceId,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAndStatusAsync(
            Guid workspaceId,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(Array.Empty<OrganicPost>());
    }

    private sealed class StubConnectionRepository : IOrganicConnectionRepository
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

    private sealed class StubPlanGenerationRepository : IEditorialPlanGenerationRepository
    {
        public Task<EditorialPlanGeneration?> GetByKeyAsync(
            Guid workspaceId,
            DateOnly rangeStart,
            DateOnly rangeEnd,
            int seed,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EditorialPlanGeneration?>(null);

        public Task AddAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(EditorialPlanGeneration generation, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubCreativeGenerator : ISocialCreativeGenerator
    {
        public Task<Result<GeneratedCopyDraft>> GenerateAsync(CopyBrief brief, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed));
    }
}
