using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Master.Application.Billing.Dunning;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Repositories;
using Master.Domain.Integrations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Acceptance and contract tests for the API health, rate limit monitoring and tenant connections endpoints.
/// </summary>
public sealed class ApiHealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of <see cref="ApiHealthEndpointTests"/>.
    /// </summary>
    /// <param name="factory">Application factory.</param>
    public ApiHealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient(List<TenantApiConnection>? connections = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDunningEngineService, FakeDunningEngineService>();
                services.AddScoped<IApiQuotaRepository, FakeApiQuotaRepository>();
                services.AddScoped<ITenantApiConnectionRepository>(_ => new FakeTenantApiConnectionRepository(connections ?? new List<TenantApiConnection>()));
            });
        }).CreateClient();
    }

    /// <summary>
    /// Verifies that GET /api/v1/admin/api-health returns 200 OK with all 4 ad platforms.
    /// </summary>
    [Fact]
    public async Task GetOverview_ShouldReturnOk_WithPlatformQuotas()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/v1/admin/api-health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<ApiHealthOverviewDto>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PlatformQuotas.Should().HaveCount(4);
        result.Value.PlatformQuotas.Select(p => p.Platform).Should().BeEquivalentTo(new[]
        {
            AdPlatform.Meta,
            AdPlatform.Google,
            AdPlatform.TikTok,
            AdPlatform.Bing
        });
    }

    /// <summary>
    /// Verifies that GET /api/v1/admin/api-health/connections returns 200 OK.
    /// </summary>
    [Fact]
    public async Task GetConnections_ShouldReturnOk()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var sampleConnection = TenantApiConnection.Create(
            Guid.NewGuid(), "Alpha Tenant", AdPlatform.Meta, "act_123", "Account 1", now.AddDays(15), now).Value;

        var client = CreateTestClient(new List<TenantApiConnection> { sampleConnection });

        // Act
        var response = await client.GetAsync("/api/v1/admin/api-health/connections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<IReadOnlyList<TenantApiConnectionDto>>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].TenantName.Should().Be("Alpha Tenant");
    }

    /// <summary>
    /// Verifies that POST /api/v1/admin/api-health/usage increments consumption and returns 200 OK.
    /// </summary>
    [Fact]
    public async Task RecordUsage_ShouldReturnOk_WhenPayloadIsValid()
    {
        // Arrange
        var client = CreateTestClient();
        var request = new RecordUsageApiRequest(AdPlatform.Meta, 120);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/admin/api-health/usage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<PlatformQuotaStatusDto>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Platform.Should().Be(AdPlatform.Meta);
        result.Value.CurrentConsumption.Should().BeGreaterThanOrEqualTo(120);
    }

    /// <summary>
    /// Verifies that POST /api/v1/admin/api-health/usage with invalid units returns 422 UnprocessableEntity.
    /// </summary>
    [Fact]
    public async Task RecordUsage_ShouldReturn422_WhenUnitsAreZeroOrNegative()
    {
        // Arrange
        var client = CreateTestClient();
        var request = new RecordUsageApiRequest(AdPlatform.Google, 0);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/admin/api-health/usage", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResultEnvelope<PlatformQuotaStatusDto>>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.IsFailure.Should().BeTrue();
    }

    private sealed class FakeDunningEngineService : IDunningEngineService
    {
        public Task<BuildingBlocks.Domain.Primitives.Result<DunningExecutionSummary>> ProcessDunningCycleAsync(
            DateTime? referenceDateUtc = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<DunningExecutionSummary>.Success(
                new DunningExecutionSummary(0, 0, 0, 0, DateTime.UtcNow)));
        }
    }

    private sealed class FakeApiQuotaRepository : IApiQuotaRepository
    {
        public Task<ApiQuotaTracker?> GetByPlatformAsync(AdPlatform platform, CancellationToken cancellationToken = default) => Task.FromResult<ApiQuotaTracker?>(null);
        public Task<IReadOnlyList<ApiQuotaTracker>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ApiQuotaTracker>>(new List<ApiQuotaTracker>());
        public Task AddAsync(ApiQuotaTracker tracker, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(ApiQuotaTracker tracker, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeTenantApiConnectionRepository : ITenantApiConnectionRepository
    {
        private readonly List<TenantApiConnection> _connections;

        public FakeTenantApiConnectionRepository(List<TenantApiConnection> connections)
        {
            _connections = connections;
        }

        public Task<TenantApiConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_connections.FirstOrDefault(c => c.Id == id));
        }

        public Task<IReadOnlyList<TenantApiConnection>> GetConnectionsAsync(
            AdPlatform? platform = null,
            ApiConnectionStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _connections.AsQueryable();
            if (platform.HasValue) query = query.Where(c => c.Platform == platform.Value);
            if (status.HasValue) query = query.Where(c => c.Status == status.Value);
            return Task.FromResult<IReadOnlyList<TenantApiConnection>>(query.ToList());
        }

        public Task<int> CountByStatusAsync(ApiConnectionStatus status, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_connections.Count(c => c.Status == status));
        }

        public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_connections.Count);
        }

        public Task AddAsync(TenantApiConnection connection, CancellationToken cancellationToken = default)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TenantApiConnection connection, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ResultEnvelope<T>
    {
        public bool IsSuccess { get; set; }
        public bool IsFailure { get; set; }
        public T? Value { get; set; }
    }
}
