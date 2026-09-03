using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para os endpoints de OpenAPI e Scalar UI.
/// </summary>
public sealed class OpenApiScalarEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Inicializa os testes com o cliente HTTP da fábrica da WebApi.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação web em memória.</param>
    public OpenApiScalarEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Valida que a especificação OpenAPI JSON v1 é gerada e expõe o endpoint de health check.
    /// </summary>
    [Fact]
    public async Task GetOpenApiJson_ShouldReturnOk_AndContainHealthEndpointSpecification()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("/api/v1/health");
        content.Should().Contain("openapi");
    }

    /// <summary>
    /// Valida que a interface interativa do Scalar UI é exposta na rota /scalar/v1.
    /// </summary>
    [Fact]
    public async Task GetScalarUi_ShouldReturnOk_AndContainScalarUiDocument()
    {
        // Act
        var response = await _client.GetAsync("/scalar/v1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("html");

        var content = await response.Content.ReadAsStringAsync();
        content.ToLowerInvariant().Should().Contain("scalar");
    }
}
