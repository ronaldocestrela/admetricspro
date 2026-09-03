using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Plans.DTOs;
using Master.Application.Repositories;
using Master.Domain.Plans;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Acceptance and contract tests for the subscription plans endpoints.
/// </summary>
public sealed class PlansEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of <see cref="PlansEndpointTests"/>.
    /// </summary>
    /// <param name="factory">Application factory.</param>
    public PlansEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that GET /api/v1/plans responds with HTTP 200 and a valid Result envelope.
    /// </summary>
    [Fact]
    public async Task GetPlans_ShouldReturnOk_WithSuccessResultEnvelope()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IPlanReadOnlyRepository, FakePlanReadOnlyRepository>();
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<PlansResultEnvelope>(content, jsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().ContainSingle(p => p.Name == "Pro Plan");
    }

    /// <summary>
    /// Verifies that plan repositories are correctly registered in the WebApi service collection.
    /// </summary>
    [Fact]
    public void WebApiHost_ShouldResolvePlanRepositories_FromDependencyInjection()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetService<IPlanRepository>();
        var readOnlyRepository = scope.ServiceProvider.GetService<IPlanReadOnlyRepository>();

        // Assert
        repository.Should().NotBeNull();
        readOnlyRepository.Should().NotBeNull();
    }

    private sealed class FakePlanReadOnlyRepository : IPlanReadOnlyRepository
    {
        public Task<IReadOnlyList<PlanDto>> ListAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PlanDto> list = new List<PlanDto>
            {
                new(
                    Guid.NewGuid(), "Pro Plan", "Desc", "Pro", 299m, 10,
                    10, 5, 25_000m, true, true, false, true, true, DateTime.UtcNow, null)
            };
            return Task.FromResult(list);
        }

        public Task<PlanDto?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PlanDto?>(null);
        }
    }

    private sealed record PlansResultEnvelope(
        bool IsSuccess,
        Error? Error,
        List<PlanDto>? Value);
}
