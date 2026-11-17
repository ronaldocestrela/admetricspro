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
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Controllers.v1;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação das rotas de listagem e revogação de conexões orgânicas (Subfase 5.1 / 2.3).
/// </summary>
public sealed class SocialOrganicConnectionsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string JwtTestKey = "social-organic-connections-test-jwt-key-min-32";

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SocialOrganicConnectionsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Listagem retorna 200 com envelope Result sem segredos.
    /// </summary>
    [Fact]
    public async Task ListConnections_ShouldReturn200WithoutSecrets()
    {
        var workspaceId = Guid.NewGuid();
        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.Instagram,
            "ig-accept",
            "Conta IG",
            "scope",
            "must-not-leak-ciphertext").Value;

        var client = CreateClient(workspaceId, [connection]);
        var response = await client.GetAsync($"/api/v1/social/connections?workspaceId={workspaceId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("must-not-leak-ciphertext");
        json.Should().NotContain("EncryptedAccessToken");

        var envelope = JsonSerializer.Deserialize<Result<IReadOnlyList<OrganicConnectionSummaryDto>>>(json, JsonOptions);
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().ContainSingle();
        envelope.Value![0].DisplayName.Should().Be("Conta IG");
    }

    /// <summary>
    /// Revogação de conexão inexistente retorna 404.
    /// </summary>
    [Fact]
    public async Task RevokeConnection_UnknownId_ShouldReturn404()
    {
        var workspaceId = Guid.NewGuid();
        var client = CreateClient(workspaceId, []);
        var unknownId = Guid.NewGuid();

        var response = await client.DeleteAsync(
            $"/api/v1/social/connections/{unknownId}?workspaceId={workspaceId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = JsonSerializer.Deserialize<Result>(await response.Content.ReadAsStringAsync(), JsonOptions);
        envelope!.Error!.Code.Should().Be(SocialErrors.ConnectionNotFound.Code);
    }

    /// <summary>
    /// Endpoints expõem atributos OpenAPI exigidos.
    /// </summary>
    [Fact]
    public void ConnectionEndpoints_ShouldDeclareOpenApiAttributes()
    {
        AssertOpenApi(typeof(SocialController), nameof(SocialController.ListOrganicConnections));
        AssertOpenApi(typeof(SocialController), nameof(SocialController.RevokeOrganicConnection));
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
        statusCodes.Should().Contain(StatusCodes.Status404NotFound);
    }

    private HttpClient CreateClient(Guid workspaceId, IReadOnlyList<OrganicConnection> connections)
    {
        var workspaceRepo = new FakeWorkspaceRepository();
        workspaceRepo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);

        var connectionRepo = new InMemoryOrganicConnectionRepository(connections);

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
                services.AddScoped<IOrganicConnectionRepository>(_ => connectionRepo);
                services.AddScoped<ISocialPublishingUnitOfWork, StubUnitOfWork>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt());
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
        public Task<Result<bool>> HasAccessToWorkspaceAsync(
            Guid userId,
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<bool>.Success(true));

        public Task<Result<IReadOnlyList<Guid>>> GetAccessibleWorkspaceIdsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<Guid>>.Success(Array.Empty<Guid>()));
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public List<Workspace> Items { get; } = [];

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

    private sealed class InMemoryOrganicConnectionRepository(IReadOnlyList<OrganicConnection> seed) : IOrganicConnectionRepository
    {
        private readonly List<OrganicConnection> _items = seed.ToList();

        public Task<OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.Id == id));

        public Task<OrganicConnection?> GetByWorkspacePlatformAndExternalAccountAsync(
            Guid workspaceId,
            OrganicPlatform platform,
            string externalAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.WorkspaceId == workspaceId &&
                c.Platform == platform &&
                c.ExternalAccountId == externalAccountId));

        public Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicConnection>>(_items.Where(c => c.WorkspaceId == workspaceId).ToList());

        public Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
        {
            _items.Add(connection);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(c => c.WorkspaceId == workspaceId));
    }

    private sealed class StubUnitOfWork : ISocialPublishingUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
