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
using SocialPublishing.Application.Media.DTOs;
using SocialPublishing.Domain.Posts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Controllers.v1;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para <c>POST /api/v1/social/media</c> (Subfase 2.4).
/// </summary>
public sealed class SocialMediaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string JwtTestKey = "social-media-test-jwt-key-minimum-32-chars";

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SocialMediaEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Sem autenticação retorna 401.
    /// </summary>
    [Fact]
    public async Task RegisterMedia_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        using var content = BuildMultipart(Guid.NewGuid(), OrganicPostFormat.FeedImage);
        var response = await client.PostAsync("/api/v1/social/media", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Sem permissão SocialMediaManage retorna 403.
    /// </summary>
    [Fact]
    public async Task RegisterMedia_WhenForbidden_ShouldReturnForbidden()
    {
        var workspaceId = Guid.NewGuid();
        var repo = new FakeWorkspaceRepository();
        repo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);

        var client = CreateClient(repo, allowMediaManage: false);
        using var content = BuildMultipart(workspaceId, OrganicPostFormat.FeedImage);
        var response = await client.PostAsync("/api/v1/social/media", content);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Proporção inválida retorna 422.
    /// </summary>
    [Fact]
    public async Task RegisterMedia_WhenAspectInvalid_ShouldReturn422()
    {
        var workspaceId = Guid.NewGuid();
        var repo = new FakeWorkspaceRepository();
        repo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);

        var client = CreateClient(
            repo,
            allowMediaManage: true,
            metadata: new OrganicMediaMetadata(1920, 1080, null));

        using var content = BuildMultipart(workspaceId, OrganicPostFormat.FeedImage);
        var response = await client.PostAsync("/api/v1/social/media", content);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Upload válido retorna 201 sem ecoar o binário.
    /// </summary>
    [Fact]
    public async Task RegisterMedia_WhenValid_ShouldReturn201WithoutBinaryPayload()
    {
        var workspaceId = Guid.NewGuid();
        var repo = new FakeWorkspaceRepository();
        repo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);
        const string secretMarker = "BINARY_SECRET_SHOULD_NOT_APPEAR";

        var client = CreateClient(
            repo,
            allowMediaManage: true,
            metadata: new OrganicMediaMetadata(1080, 1080, null));

        using var content = BuildMultipart(workspaceId, OrganicPostFormat.FeedImage, secretMarker);
        var response = await client.PostAsync("/api/v1/social/media", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(secretMarker);
        body.Should().Contain("objectKey");

        var envelope = JsonSerializer.Deserialize<Result<OrganicMediaAssetDto>>(body, JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value!.Width.Should().Be(1080);
    }

    /// <summary>
    /// O endpoint expõe atributos OpenAPI exigidos.
    /// </summary>
    [Fact]
    public void RegisterMedia_ShouldDeclareOpenApiAttributes()
    {
        var method = typeof(SocialController).GetMethod(
            nameof(SocialController.RegisterMedia),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        method.Should().NotBeNull();
        method!.GetCustomAttributes<EndpointSummaryAttribute>().Should().NotBeEmpty();
        method.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .Should().Contain(StatusCodes.Status201Created);
    }

    private HttpClient CreateClient(
        FakeWorkspaceRepository workspaceRepo,
        bool allowMediaManage,
        OrganicMediaMetadata? metadata = null)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["TenantJwt:SecretKey"] = JwtTestKey }));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<Tenants.Application.Rbac.Services.IPermissionEvaluator>(_ =>
                    new TestPermissionEvaluator(allowMediaManage));
                services.AddScoped<Tenants.Application.Squads.Services.IUserPortfolioService, TestPortfolio>();
                services.AddScoped<IWorkspaceRepository>(_ => workspaceRepo);
                services.AddSingleton<IOrganicMediaStore, AcceptanceFakeOrganicMediaStore>();
                services.AddSingleton<IOrganicMediaMetadataReader>(
                    _ => new AcceptanceFixedMetadataReader(metadata ?? new OrganicMediaMetadata(1080, 1080, null)));
                services.AddScoped<IOrganicMediaAssetRepository, AcceptanceStubMediaAssetRepository>();
                services.AddScoped<ISocialPublishingUnitOfWork, AcceptanceStubSocialUnitOfWork>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());
        return client;
    }

    private static MultipartFormDataContent BuildMultipart(
        Guid workspaceId,
        OrganicPostFormat format,
        string fileContent = "fake-image-bytes")
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(workspaceId.ToString()), "workspaceId" },
            { new StringContent(((int)format).ToString()), "format" }
        };

        var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        var fileContentPart = new ByteArrayContent(fileBytes);
        fileContentPart.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContentPart, "file", "test.jpg");
        return content;
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

    private sealed class TestPermissionEvaluator(bool allowMediaManage)
        : Tenants.Application.Rbac.Services.IPermissionEvaluator
    {
        public Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            TenantPermission permission,
            Tenants.Application.Rbac.Models.PermissionContext? context = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(
                allowMediaManage && permission == TenantPermission.SocialMediaManage));

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

    private sealed class AcceptanceFakeOrganicMediaStore : IOrganicMediaStore
    {
        private readonly Dictionary<string, byte[]> _objects = new();

        public Task<Result<string>> SaveAsync(
            Guid tenantId,
            Guid workspaceId,
            Guid assetId,
            Stream content,
            string contentType,
            OrganicMediaStorageTier tier,
            CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var key = tier == OrganicMediaStorageTier.Staging
                ? $"staging/{assetId:N}"
                : $"durable/{tenantId:N}/{workspaceId:N}/{assetId:N}";
            _objects[key] = ms.ToArray();
            return Task.FromResult(Result<string>.Success(key));
        }

        public Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(objectKey, out var bytes))
            {
                return Task.FromResult(Result<Stream>.Failure(Error.NotFound("test", "missing")));
            }

            return Task.FromResult(Result<Stream>.Success(new MemoryStream(bytes, writable: false)));
        }

        public Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            _objects.Remove(objectKey);
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class AcceptanceFixedMetadataReader(OrganicMediaMetadata metadata) : IOrganicMediaMetadataReader
    {
        public Task<Result<OrganicMediaMetadata>> ReadAsync(
            Stream content,
            string mimeType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<OrganicMediaMetadata>.Success(metadata));
    }

    private sealed class AcceptanceStubMediaAssetRepository : IOrganicMediaAssetRepository
    {
        public Task<OrganicMediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganicMediaAsset?>(null);

        public Task AddAsync(OrganicMediaAsset asset, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AcceptanceStubSocialUnitOfWork : ISocialPublishingUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
