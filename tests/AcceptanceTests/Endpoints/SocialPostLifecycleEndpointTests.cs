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
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Controllers.v1;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação das rotas de aprovação interna e reagendamento (Subfase 4.1).
/// </summary>
public sealed class SocialPostLifecycleEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string JwtTestKey = "social-post-lifecycle-test-jwt-key-min-32-chars";

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SocialPostLifecycleEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Submit move Draft para PendingApproval.
    /// </summary>
    [Fact]
    public async Task SubmitPost_FromDraft_ShouldReturn200WithPendingApproval()
    {
        var workspaceId = Guid.NewGuid();
        var post = CreateDraftPost(workspaceId);
        var client = CreateClient(workspaceId, post);

        var response = await client.PostAsync(
            $"/api/v1/social/posts/{post.Id}:submit?workspaceId={workspaceId}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = DeserializePost(await response.Content.ReadAsStringAsync());
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value!.Status.Should().Be(OrganicPostStatus.PendingApproval);
    }

    /// <summary>
    /// Schedule a partir de Approved retorna 200 com Scheduled.
    /// </summary>
    [Fact]
    public async Task SchedulePost_FromApproved_ShouldReturn200WithScheduled()
    {
        var workspaceId = Guid.NewGuid();
        var post = CreateDraftPost(workspaceId);
        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        post.Approve().IsSuccess.Should().BeTrue();
        var client = CreateClient(workspaceId, post);

        var response = await client.PostAsync(
            $"/api/v1/social/posts/{post.Id}:schedule?workspaceId={workspaceId}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = DeserializePost(await response.Content.ReadAsStringAsync());
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value!.Status.Should().Be(OrganicPostStatus.Scheduled);
    }

    /// <summary>
    /// Schedule a partir de Draft retorna 409.
    /// </summary>
    [Fact]
    public async Task SchedulePost_FromDraft_ShouldReturn409()
    {
        var workspaceId = Guid.NewGuid();
        var post = CreateDraftPost(workspaceId);
        var client = CreateClient(workspaceId, post);

        var response = await client.PostAsync(
            $"/api/v1/social/posts/{post.Id}:schedule?workspaceId={workspaceId}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = DeserializePost(await response.Content.ReadAsStringAsync());
        envelope!.Error!.Code.Should().Be(SocialErrors.InvalidTransition.Code);
    }

    /// <summary>
    /// Approve ilegal retorna 409.
    /// </summary>
    [Fact]
    public async Task ApprovePost_FromDraft_ShouldReturn409()
    {
        var workspaceId = Guid.NewGuid();
        var post = CreateDraftPost(workspaceId);
        var client = CreateClient(workspaceId, post);

        var response = await client.PostAsync(
            $"/api/v1/social/posts/{post.Id}:approve?workspaceId={workspaceId}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = DeserializePost(await response.Content.ReadAsStringAsync());
        envelope!.Error!.Code.Should().Be(SocialErrors.InvalidTransition.Code);
    }

    /// <summary>
    /// Reagendamento inválido em Scheduled no passado retorna 422.
    /// </summary>
    [Fact]
    public async Task ReschedulePost_ScheduledInPast_ShouldReturn422()
    {
        var workspaceId = Guid.NewGuid();
        var post = CreateDraftPost(workspaceId);
        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        post.Approve().IsSuccess.Should().BeTrue();
        post.Schedule().IsSuccess.Should().BeTrue();
        var client = CreateClient(workspaceId, post, fixedUtcNow: new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        var body = new StringContent(
            JsonSerializer.Serialize(new { scheduledAtUtc = "2026-06-10T12:00:00Z" }),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(
            $"/api/v1/social/posts/{post.Id}:reschedule?workspaceId={workspaceId}",
            body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var envelope = DeserializePost(await response.Content.ReadAsStringAsync());
        envelope!.Error!.Code.Should().Be(SocialErrors.ScheduleInThePast.Code);
    }

    /// <summary>
    /// Post inexistente retorna 404.
    /// </summary>
    [Fact]
    public async Task SubmitPost_UnknownId_ShouldReturn404()
    {
        var workspaceId = Guid.NewGuid();
        var client = CreateClient(workspaceId);

        var response = await client.PostAsync(
            $"/api/v1/social/posts/{Guid.NewGuid()}:submit?workspaceId={workspaceId}",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = DeserializePost(await response.Content.ReadAsStringAsync());
        envelope!.Error!.Code.Should().Be(SocialErrors.PostNotFound.Code);
    }

    /// <summary>
    /// Endpoints expõem atributos OpenAPI exigidos.
    /// </summary>
    [Fact]
    public void PostLifecycleEndpoints_ShouldDeclareOpenApiAttributes()
    {
        AssertOpenApi(typeof(SocialController), nameof(SocialController.SubmitPostForApproval));
        AssertOpenApi(typeof(SocialController), nameof(SocialController.ApprovePost));
        AssertOpenApi(typeof(SocialController), nameof(SocialController.SchedulePost));
        AssertOpenApi(typeof(SocialController), nameof(SocialController.RejectPost));
        AssertOpenApi(typeof(SocialController), nameof(SocialController.ReschedulePost));
    }

    private static Result<OrganicPostDto>? DeserializePost(string json) =>
        JsonSerializer.Deserialize<Result<OrganicPostDto>>(json, JsonOptions);

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
        statusCodes.Should().Contain(StatusCodes.Status409Conflict);
        statusCodes.Should().Contain(StatusCodes.Status422UnprocessableEntity);
    }

    private static OrganicPost CreateDraftPost(Guid workspaceId) =>
        OrganicPost.CreateEditorialDraft(
            workspaceId,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            global::SocialPublishing.Domain.Brand.ContentPillar.Authority,
            "Legenda",
            new DateTime(2026, 6, 2, 10, 0, 0),
            "America/Sao_Paulo").Value;

    private HttpClient CreateClient(
        Guid workspaceId,
        OrganicPost? seededPost = null,
        DateTime? fixedUtcNow = null)
    {
        var workspaceRepo = new FakeWorkspaceRepository();
        workspaceRepo.Items.Add(Workspace.Create(workspaceId, "Cliente", "123.456.789-09", 1000m, "Varejo").Value);

        var postRepo = new LifecycleInMemoryPostRepository();
        if (seededPost is not null)
        {
            postRepo.AllPosts.Add(seededPost);
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
                services.AddScoped<IOrganicPostRepository>(_ => postRepo);
                services.AddScoped<IPublicationJobRepository, AcceptancePublicationJobRepository>();
                services.AddScoped<ISocialPublishingUnitOfWork, StubUnitOfWork>();
                if (fixedUtcNow.HasValue)
                {
                    services.AddSingleton<TimeProvider>(new FixedTimeProvider(fixedUtcNow.Value));
                }
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

    private sealed class LifecycleInMemoryPostRepository : IOrganicPostRepository
    {
        public List<OrganicPost> AllPosts { get; } = new();

        public Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllPosts.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(AllPosts.Where(p => p.WorkspaceId == workspaceId).ToList());

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
            Task.FromResult<IReadOnlyList<OrganicPost>>(
                AllPosts.Where(p => p.WorkspaceId == workspaceId && p.Status == status).ToList());
    }

    private sealed class StubUnitOfWork : ISocialPublishingUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class AcceptancePublicationJobRepository : IPublicationJobRepository
    {
        private readonly List<PublicationJob> _jobs = new();

        public Task<PublicationJob?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_jobs.FirstOrDefault(j => j.IdempotencyKey == idempotencyKey));

        public Task<PublicationJob?> GetActiveByPostIdAsync(Guid postId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _jobs.FirstOrDefault(j =>
                    j.PostId == postId &&
                    j.Status is PublicationJobStatus.Pending or PublicationJobStatus.RetryScheduled or PublicationJobStatus.InProgress));

        public Task<Result> AddAsync(PublicationJob job, CancellationToken cancellationToken = default)
        {
            if (_jobs.Any(j => j.IdempotencyKey == job.IdempotencyKey))
            {
                return Task.FromResult(Result.Failure(SocialErrors.PublicationJobAlreadyExists));
            }

            _jobs.Add(job);
            return Task.FromResult(Result.Success());
        }

        public Task UpdateAsync(PublicationJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PublicationJob?> TryClaimAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PublicationJob?>(null);

        public Task<IReadOnlyList<PublicationJob>> ListDueAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicationJob>>(Array.Empty<PublicationJob>());
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
