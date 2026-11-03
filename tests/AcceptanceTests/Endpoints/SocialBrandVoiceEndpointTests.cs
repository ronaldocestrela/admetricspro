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
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Controllers.v1;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para rotas de Brand Voice (Subfase 3.1).
/// </summary>
public sealed class SocialBrandVoiceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly AcceptanceInMemoryBrandVoiceRepository _brandVoiceRepository = new();
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string JwtTestKey = "social-brand-voice-test-jwt-key-min-32";

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SocialBrandVoiceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Sem autenticação retorna 401 no PUT.
    /// </summary>
    [Fact]
    public async Task UpsertBrandVoice_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PutAsync(
            $"/api/v1/social/brand-voice/{Guid.NewGuid()}",
            BuildJsonBody(ValidRequest()));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// PUT sem permissão de copiloto retorna 403.
    /// </summary>
    [Fact]
    public async Task UpsertBrandVoice_WhenForbidden_ShouldReturnForbidden()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId, allowSocialRead: true, allowCopilotGenerate: false);
        var response = await client.PutAsync(
            $"/api/v1/social/brand-voice/{workspaceId}",
            BuildJsonBody(ValidRequest()));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// GET sem social.read retorna 403.
    /// </summary>
    [Fact]
    public async Task GetBrandVoice_WhenReadForbidden_ShouldReturnForbidden()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId, allowSocialRead: false, allowCopilotGenerate: true);
        var response = await client.GetAsync($"/api/v1/social/brand-voice/{workspaceId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Workspace inexistente retorna 404.
    /// </summary>
    [Fact]
    public async Task UpsertBrandVoice_WhenWorkspaceMissing_ShouldReturnNotFound()
    {
        var workspaceId = Guid.NewGuid();
        var client = CreateClient(workspaceId, allowSocialRead: true, allowCopilotGenerate: true, registerWorkspace: false);
        var response = await client.PutAsync(
            $"/api/v1/social/brand-voice/{workspaceId}",
            BuildJsonBody(ValidRequest()));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// PUT válido retorna 200 com envelope Result.
    /// </summary>
    [Fact]
    public async Task UpsertBrandVoice_WhenValid_ShouldReturn200WithSameIdOnSecondPut()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId, allowSocialRead: true, allowCopilotGenerate: true);

        var first = await client.PutAsync(
            $"/api/v1/social/brand-voice/{workspaceId}",
            BuildJsonBody(ValidRequest()));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await Deserialize<BrandVoiceDto>(first);
        firstBody!.Tone.Should().Be("Tom API");

        var secondRequest = ValidRequest() with { Tone = "Tom revisado API" };
        var second = await client.PutAsync(
            $"/api/v1/social/brand-voice/{workspaceId}",
            BuildJsonBody(secondRequest));
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await Deserialize<BrandVoiceDto>(second);
        secondBody!.Id.Should().Be(firstBody.Id);
        secondBody.Tone.Should().Be("Tom revisado API");
    }

    /// <summary>
    /// Pesos inválidos retornam 422.
    /// </summary>
    [Fact]
    public async Task UpsertBrandVoice_WhenWeightsInvalid_ShouldReturn422()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId, allowSocialRead: true, allowCopilotGenerate: true);
        var invalid = ValidRequest() with { AuthorityWeight = 30, SaleWeight = 30, ConnectionWeight = 30, SocialProofWeight = 30 };
        var response = await client.PutAsync(
            $"/api/v1/social/brand-voice/{workspaceId}",
            BuildJsonBody(invalid));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var envelope = JsonSerializer.Deserialize<Result<BrandVoiceDto>>(await response.Content.ReadAsStringAsync(), JsonOptions);
        envelope!.Error!.Code.Should().Be(SocialErrors.PillarWeightsInvalid.Code);
    }

    /// <summary>
    /// GET sem perfil retorna 404 BrandVoiceNotFound.
    /// </summary>
    [Fact]
    public async Task GetBrandVoice_WhenMissing_ShouldReturn404()
    {
        var workspaceId = SeedWorkspace();
        var client = CreateClient(workspaceId, allowSocialRead: true, allowCopilotGenerate: true);
        var response = await client.GetAsync($"/api/v1/social/brand-voice/{workspaceId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = JsonSerializer.Deserialize<Result<BrandVoiceDto>>(await response.Content.ReadAsStringAsync(), JsonOptions);
        envelope!.Error!.Code.Should().Be(SocialErrors.BrandVoiceNotFound.Code);
    }

    /// <summary>
    /// GET com perfil existente retorna 200 apenas com social.read.
    /// </summary>
    [Fact]
    public async Task GetBrandVoice_WhenProfileExists_ShouldReturn200WithSocialReadOnly()
    {
        var workspaceId = SeedWorkspace();
        var writeClient = CreateClient(workspaceId, allowSocialRead: true, allowCopilotGenerate: true);
        await writeClient.PutAsync(
            $"/api/v1/social/brand-voice/{workspaceId}",
            BuildJsonBody(ValidRequest()));

        var readClient = CreateClient(workspaceId, allowSocialRead: true, allowCopilotGenerate: false);
        var response = await readClient.GetAsync($"/api/v1/social/brand-voice/{workspaceId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await Deserialize<BrandVoiceDto>(response);
        dto!.WorkspaceId.Should().Be(workspaceId);
    }

    /// <summary>
    /// Endpoints expõem atributos OpenAPI exigidos.
    /// </summary>
    [Fact]
    public void BrandVoiceEndpoints_ShouldDeclareOpenApiAttributes()
    {
        AssertOpenApi(typeof(SocialController), nameof(SocialController.UpsertBrandVoice));
        AssertOpenApi(typeof(SocialController), nameof(SocialController.GetBrandVoice));
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
        statusCodes.Should().Contain(StatusCodes.Status404NotFound);
        statusCodes.Should().Contain(StatusCodes.Status422UnprocessableEntity);
    }

    private Guid SeedWorkspace()
    {
        var workspaceId = Guid.NewGuid();
        _ = workspaceId;
        return workspaceId;
    }

    private HttpClient CreateClient(
        Guid workspaceId,
        bool allowSocialRead,
        bool allowCopilotGenerate,
        bool registerWorkspace = true)
    {
        var workspaceRepo = new FakeWorkspaceRepository();
        if (registerWorkspace)
        {
            workspaceRepo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);
        }

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["TenantJwt:SecretKey"] = JwtTestKey }));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<Tenants.Application.Rbac.Services.IPermissionEvaluator>(_ =>
                    new TestPermissionEvaluator(allowSocialRead, allowCopilotGenerate));
                services.AddScoped<Tenants.Application.Squads.Services.IUserPortfolioService, TestPortfolio>();
                services.AddScoped<IWorkspaceRepository>(_ => workspaceRepo);
                services.AddSingleton(_brandVoiceRepository);
                services.AddScoped<IBrandVoiceRepository>(sp => sp.GetRequiredService<AcceptanceInMemoryBrandVoiceRepository>());
                services.AddScoped<ISocialPublishingUnitOfWork, AcceptanceStubSocialUnitOfWork>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt());
        return client;
    }

    private static UpsertBrandVoiceRequest ValidRequest() =>
        new(
            "Tom API",
            "Persona API",
            ["https://example.com/about"],
            ["proibido"],
            EmojiPolicy.Sparingly,
            15,
            40,
            20,
            25,
            15,
            "pt-BR");

    private static StringContent BuildJsonBody(UpsertBrandVoiceRequest request) =>
        new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

    private static async Task<BrandVoiceDto?> Deserialize<T>(HttpResponseMessage response)
    {
        var envelope = JsonSerializer.Deserialize<Result<BrandVoiceDto>>(await response.Content.ReadAsStringAsync(), JsonOptions);
        return envelope?.Value;
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

    private sealed class TestPermissionEvaluator(bool allowSocialRead, bool allowCopilotGenerate)
        : Tenants.Application.Rbac.Services.IPermissionEvaluator
    {
        public Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            TenantPermission permission,
            Tenants.Application.Rbac.Models.PermissionContext? context = null,
            CancellationToken cancellationToken = default)
        {
            var allowed = permission switch
            {
                TenantPermission.SocialRead => allowSocialRead,
                TenantPermission.SocialCopilotGenerate => allowCopilotGenerate,
                _ => false
            };
            return Task.FromResult(Result<bool>.Success(allowed));
        }

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
}
