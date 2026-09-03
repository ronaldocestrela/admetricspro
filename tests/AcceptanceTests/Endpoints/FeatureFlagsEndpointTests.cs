using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Master.Application.Billing.Dunning;
using Master.Application.FeatureFlags.DTOs;
using Master.Application.FeatureFlags.Repositories;
using Master.Domain.FeatureFlags;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Acceptance and contract tests for the Feature Flags and Kill Switches operational endpoints.
/// </summary>
public sealed class FeatureFlagsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureFlagsEndpointTests"/>.
    /// </summary>
    /// <param name="factory">Web application factory.</param>
    public FeatureFlagsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient(List<FeatureFlag>? initialFlags = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDunningEngineService, FakeDunningEngineService>();
                var fakeRepo = new FakeFeatureFlagRepository(initialFlags);
                services.AddScoped<IFeatureFlagRepository>(_ => fakeRepo);
                services.AddScoped<Master.Application.Auditing.IMasterAuditService, FakeMasterAuditService>();
            });
        }).CreateClient();
    }

    /// <summary>
    /// Verifies that GET /api/v1/admin/feature-flags returns 200 OK with list of flags.
    /// </summary>
    [Fact]
    public async Task GetAll_ShouldReturnOk_WithFeatureFlagsList()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/v1/admin/feature-flags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("isSuccess\":true");
    }

    /// <summary>
    /// Verifies that activating and deactivating a Kill Switch transitions the operational status.
    /// </summary>
    [Fact]
    public async Task KillSwitch_Lifecycle_ShouldActivateAndDeactivateSuccessfully()
    {
        // Arrange
        var globalKillSwitch = FeatureFlag.CreateKillSwitch(
            "killswitch.automation.global",
            "Global Automation Kill Switch",
            "Emergency freeze",
            "test-suite").Value;

        var client = CreateTestClient(new List<FeatureFlag> { globalKillSwitch });

        // 1. Activate Kill Switch
        var activateResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/feature-flags/killswitch.automation.global/kill-switch/activate",
            new ActivateKillSwitchApiRequest(
                Reason: "Instabilidade severa na Meta Graph API",
                TriggeredBy: "oncall-lead"));

        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Check Automation Status
        var statusResponse = await client.GetAsync("/api/v1/admin/feature-flags/automation-status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var statusJson = await statusResponse.Content.ReadAsStringAsync();
        statusJson.Should().Contain("\"isFrozen\":true");

        // 3. Deactivate Kill Switch
        var deactivateResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/feature-flags/killswitch.automation.global/kill-switch/deactivate",
            new DeactivateKillSwitchApiRequest(
                Reason: "Incidente resolvido pela equipe Meta",
                TriggeredBy: "oncall-lead"));

        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Check Automation Status restored
        var restoredStatusResponse = await client.GetAsync("/api/v1/admin/feature-flags/automation-status");
        var restoredJson = await restoredStatusResponse.Content.ReadAsStringAsync();
        restoredJson.Should().Contain("\"isFrozen\":false");
    }

    /// <summary>
    /// Verifies that activating a Kill Switch with empty reason returns 422 UnprocessableEntity.
    /// </summary>
    [Fact]
    public async Task ActivateKillSwitch_ShouldReturn422_WhenReasonIsEmpty()
    {
        // Arrange
        var globalKillSwitch = FeatureFlag.CreateKillSwitch(
            "killswitch.automation.global",
            "Global Automation Kill Switch",
            "Emergency freeze",
            "test-suite").Value;

        var client = CreateTestClient(new List<FeatureFlag> { globalKillSwitch });

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/feature-flags/killswitch.automation.global/kill-switch/activate",
            new ActivateKillSwitchApiRequest(Reason: ""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
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

    private sealed class FakeMasterAuditService : Master.Application.Auditing.IMasterAuditService
    {
        public Task<BuildingBlocks.Domain.Primitives.Result<Guid>> RecordAsync(
            string action,
            string resource,
            string? resourceId = null,
            string? details = null,
            Guid? tenantId = null,
            string? ipAddress = null,
            IEnumerable<string>? additionalTags = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<Guid>.Success(Guid.NewGuid()));
        }
    }
}

/// <summary>
/// In-memory fake repository for acceptance test scenarios.
/// </summary>
internal sealed class FakeFeatureFlagRepository : IFeatureFlagRepository
{
    private readonly List<FeatureFlag> _flags;

    public FakeFeatureFlagRepository(List<FeatureFlag>? initialFlags = null)
    {
        _flags = initialFlags ?? new List<FeatureFlag>
        {
            FeatureFlag.CreateKillSwitch("killswitch.automation.global", "Global", "Desc", "seed").Value,
            FeatureFlag.CreateKillSwitch("killswitch.automation.meta", "Meta", "Desc", "seed").Value,
            FeatureFlag.Create("feature.analytics.mer-v2", "MER v2", "Desc", true, false, FeatureFlagTargetingType.PercentageRollout, 20, null, "seed").Value
        };
    }

    public Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim().ToLowerInvariant();
        return Task.FromResult(_flags.FirstOrDefault(f => f.Key == normalized));
    }

    public Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_flags.FirstOrDefault(f => f.Id == id));
    }

    public Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FeatureFlag>>(_flags.ToList());
    }

    public Task<IReadOnlyList<FeatureFlag>> GetKillSwitchesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FeatureFlag>>(_flags.Where(f => f.IsKillSwitch).ToList());
    }

    public Task AddAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        _flags.Add(flag);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        var idx = _flags.FindIndex(f => f.Id == flag.Id || f.Key == flag.Key);
        if (idx >= 0)
            _flags[idx] = flag;
        else
            _flags.Add(flag);

        return Task.CompletedTask;
    }
}
