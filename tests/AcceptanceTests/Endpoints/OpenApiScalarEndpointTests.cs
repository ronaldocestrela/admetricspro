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
    /// Valida que a interface interativa do Scalar UI é exposta na rota /scalar/v1 e configurada para suporte corporativo.
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
        content.Should().Contain("AdMetricsPro API - Scalar Reference");
    }

    /// <summary>
    /// Valida que a especificação OpenAPI JSON v1 expõe o esquema de autenticação corporativa Bearer JWT e requisito de segurança global.
    /// </summary>
    [Fact]
    public async Task GetOpenApiJson_ShouldContainCorporateBearerSecuritySchemeAndRequirement()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Validação de components.securitySchemes.Bearer
        root.TryGetProperty("components", out var components).Should().BeTrue();
        components.TryGetProperty("securitySchemes", out var securitySchemes).Should().BeTrue();
        securitySchemes.TryGetProperty("Bearer", out var bearerScheme).Should().BeTrue();

        bearerScheme.GetProperty("type").GetString().Should().Be("http");
        bearerScheme.GetProperty("scheme").GetString().Should().Be("bearer");
        bearerScheme.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        // Validação de requisitos de segurança globais
        root.TryGetProperty("security", out var security).Should().BeTrue();
        security.GetArrayLength().Should().BeGreaterThan(0, $"Security raw JSON: {security.GetRawText()}");
        var hasBearerRequirement = false;
        foreach (var req in security.EnumerateArray())
        {
            foreach (var prop in req.EnumerateObject())
            {
                if (prop.Name.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    hasBearerRequirement = true;
                    break;
                }
            }
        }
        hasBearerRequirement.Should().BeTrue($"Security section was: {security.GetRawText()}");
    }

    /// <summary>
    /// Valida que todos os endpoints administrativos do Backoffice estão presentes na especificação OpenAPI v1.
    /// </summary>
    [Fact]
    public async Task GetOpenApiJson_ShouldContainAllAdministrativeBackofficeRoutes()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var root = doc.RootElement;
        root.TryGetProperty("paths", out var paths).Should().BeTrue();

        var expectedRoutes = new[]
        {
            "/api/v1/health",
            "/api/v1/plans",
            "/api/v1/plans/{id}",
            "/api/v1/tenants/{tenantId}/impersonate",
            "/api/v1/tenants/{tenantId}/impersonate/{sessionId}/terminate",
            "/api/v1/admin/api-health",
            "/api/v1/admin/api-health/connections",
            "/api/v1/admin/api-health/usage",
            "/api/v1/billing/dunning/execute",
            "/api/v1/admin/feature-flags",
            "/api/v1/admin/feature-flags/{key}",
            "/api/v1/admin/feature-flags/{key}/kill-switch/activate",
            "/api/v1/admin/feature-flags/{key}/kill-switch/deactivate",
            "/api/v1/admin/feature-flags/{key}/evaluate",
            "/api/v1/admin/feature-flags/automation-status"
        };

        foreach (var route in expectedRoutes)
        {
            paths.TryGetProperty(route, out _).Should().BeTrue($"A rota administrativa '{route}' deve estar registrada no OpenAPI.");
        }
    }

    /// <summary>
    /// Valida que todos os endpoints registrados contêm [EndpointSummary] preenchido e respostas tipadas.
    /// </summary>
    [Fact]
    public async Task GetOpenApiJson_ShouldContainSemanticSummariesAndResponseCodesForAllOperations()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        using var doc = System.Text.Json.JsonDocument.Parse(content);
        var paths = doc.RootElement.GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                // Verifica apenas métodos HTTP conhecidos
                var method = operation.Name.ToLowerInvariant();
                if (method is not ("get" or "post" or "put" or "delete" or "patch"))
                    continue;

                operation.Value.TryGetProperty("summary", out var summary).Should().BeTrue(
                    $"A operação {operation.Name.ToUpperInvariant()} em '{path.Name}' deve possuir [EndpointSummary].");
                summary.GetString().Should().NotBeNullOrWhiteSpace(
                    $"O resumo da operação {operation.Name.ToUpperInvariant()} em '{path.Name}' não pode ser vazio.");

                operation.Value.TryGetProperty("responses", out var responses).Should().BeTrue(
                    $"A operação {operation.Name.ToUpperInvariant()} em '{path.Name}' deve declarar respostas HTTP.");
                responses.EnumerateObject().Count().Should().BeGreaterThan(0,
                    $"A operação {operation.Name.ToUpperInvariant()} em '{path.Name}' deve conter códigos de status HTTP mapeados.");
            }
        }
    }
}
