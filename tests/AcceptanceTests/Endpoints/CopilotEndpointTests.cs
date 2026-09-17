using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Analytics.Application.Copilot.DTOs;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de contrato e aceitação para os endpoints do Copiloto de IA (/api/v1/analytics/copilot).
/// </summary>
public sealed class CopilotEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Inicializa a suíte de testes com a fábrica de aplicação Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação Web.</param>
    public CopilotEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida que a consulta de diagnóstico sem identificação de tenant retorna 400 com envelope Result estruturado em JSON.
    /// </summary>
    [Fact]
    public async Task GetCopilotDiagnostic_WithoutTenantHeader_ShouldReturnBadRequestWithResultJson()
    {
        // Arrange
        var client = _factory.CreateClient();
        var workspaceId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/analytics/copilot/diagnostic?workspaceId={workspaceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await response.Content.ReadFromJsonAsync<Result<DailyDiagnosticReportDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsFailure.Should().BeTrue();
        envelope.Error.Code.Should().Be("Tenant.ContextNotResolved");
    }

    /// <summary>
    /// Valida que a consulta de diagnóstico com WorkspaceId vazio retorna 400 com envelope Result estruturado em JSON.
    /// </summary>
    [Fact]
    public async Task GetCopilotDiagnostic_WithEmptyWorkspace_ShouldReturnBadRequestWithResultJson()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.NewGuid().ToString());

        // Act
        var response = await client.GetAsync($"/api/v1/analytics/copilot/diagnostic?workspaceId={Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await response.Content.ReadFromJsonAsync<Result<DailyDiagnosticReportDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsFailure.Should().BeTrue();
        envelope.Error.Code.Should().Be("Copilot.InvalidWorkspaceId");
    }
}
