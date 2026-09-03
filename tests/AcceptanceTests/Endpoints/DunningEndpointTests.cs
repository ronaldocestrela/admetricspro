using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Master.Application.Billing.Dunning;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Acceptance and contract tests for the billing and dunning engine endpoints.
/// </summary>
public sealed class DunningEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of <see cref="DunningEndpointTests"/>.
    /// </summary>
    /// <param name="factory">Application factory.</param>
    public DunningEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that POST /api/v1/billing/dunning/execute triggers the dunning cycle and returns HTTP 200 with valid Result envelope.
    /// </summary>
    [Fact]
    public async Task ExecuteDunningCycle_ShouldReturnOk_WithSuccessResultEnvelope()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDunningEngineService, FakeDunningEngineService>();
            });
        }).CreateClient();

        var request = new ExecuteDunningApiRequest(DateTime.UtcNow);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/billing/dunning/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<DunningResultEnvelope>(content, jsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EvaluatedCount.Should().Be(5);
        result.Value.TransitionsCount.Should().Be(2);
        result.Value.SuspendedCount.Should().Be(1);
    }

    private sealed class FakeDunningEngineService : IDunningEngineService
    {
        public Task<BuildingBlocks.Domain.Primitives.Result<DunningExecutionSummary>> ProcessDunningCycleAsync(
            DateTime? referenceDateUtc = null,
            CancellationToken cancellationToken = default)
        {
            var summary = new DunningExecutionSummary(5, 2, 1, 2, referenceDateUtc ?? DateTime.UtcNow);
            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<DunningExecutionSummary>.Success(summary));
        }
    }

    private sealed record DunningResultEnvelope
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public DunningExecutionSummaryResponse? Value { get; init; }
    }
}
