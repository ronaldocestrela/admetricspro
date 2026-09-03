using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de contrato e aceitação para o endpoint de integridade operacional (Health Check).
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Inicializa uma nova instância de testes utilizando a fábrica da WebApi.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação web em memória.</param>
    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Garante que o endpoint de health check responda HTTP 200 com envelope Result de sucesso.
    /// </summary>
    [Fact]
    public async Task GetHealth_ShouldReturnOk_WithSuccessResultEnvelope()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<HealthResultEnvelope>(content, jsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().BeEmpty();
        result.Error.Description.Should().BeEmpty();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Healthy");
        result.Value.Service.Should().Be("AdMetricsPro API");
        result.Value.TimestampUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    private sealed record HealthResultEnvelope(
        bool IsSuccess,
        Error? Error,
        HealthPayload? Value);

    private sealed record HealthPayload(
        string Status,
        DateTime TimestampUtc,
        string Service,
        string Environment);
}
