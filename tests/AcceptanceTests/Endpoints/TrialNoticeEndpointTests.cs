using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Master.Application.Billing.Trial;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para o endpoint de execução da régua de trial (POST /api/v1/billing/trial-notices/execute).
/// </summary>
public sealed class TrialNoticeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TrialNoticeEndpointTests"/>.
    /// </summary>
    /// <param name="factory">Fábrica da aplicação web.</param>
    public TrialNoticeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida que o endpoint POST /api/v1/billing/trial-notices/execute dispara o ciclo e retorna HTTP 200 com envelope de sucesso.
    /// </summary>
    [Fact]
    public async Task ExecuteTrialNoticeCycle_ShouldReturnOk_WithSuccessResultEnvelope()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ITrialNotificationEngineService, FakeTrialNotificationEngineService>();
            });
        }).CreateClient();

        var request = new ExecuteTrialNoticeApiRequest(DateTime.UtcNow);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/billing/trial-notices/execute", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<TrialNoticeResultEnvelope>(content, jsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EvaluatedCount.Should().Be(8);
        result.Value.SevenDayNoticesSent.Should().Be(3);
        result.Value.ThreeDayNoticesSent.Should().Be(2);
        result.Value.OneDayNoticesSent.Should().Be(1);
        result.Value.ExpiredNoticesSent.Should().Be(0);
        result.Value.TotalNoticesSent.Should().Be(6);
    }

    private sealed class FakeTrialNotificationEngineService : ITrialNotificationEngineService
    {
        public Task<BuildingBlocks.Domain.Primitives.Result<TrialNoticeExecutionSummary>> ProcessTrialNoticesCycleAsync(
            DateTime? referenceDateUtc = null,
            CancellationToken cancellationToken = default)
        {
            var summary = new TrialNoticeExecutionSummary(
                EvaluatedCount: 8,
                SevenDayNoticesSent: 3,
                ThreeDayNoticesSent: 2,
                OneDayNoticesSent: 1,
                ExpiredNoticesSent: 0,
                FailuresCount: 0,
                ExecutedAtUtc: referenceDateUtc ?? DateTime.UtcNow);

            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<TrialNoticeExecutionSummary>.Success(summary));
        }
    }

    private sealed record TrialNoticeResultEnvelope
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public TrialNoticeExecutionSummaryResponse? Value { get; init; }
    }
}
