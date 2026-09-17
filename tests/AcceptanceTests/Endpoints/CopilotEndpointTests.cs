using System.Net;
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

    /// <summary>
    /// Inicializa a suíte de testes com a fábrica de aplicação Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação Web.</param>
    public CopilotEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Diagnostica o comportamento do endpoint GetDiagnostic quando chamado via WebApplicationFactory.
    /// </summary>
    [Fact]
    public async Task GetCopilotDiagnostic_ShouldReturnResponse()
    {
        var client = _factory.CreateClient();
        var workspaceId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.NewGuid().ToString());
        var response = await client.GetAsync($"/api/v1/analytics/copilot/diagnostic?workspaceId={workspaceId}");

        var content = await response.Content.ReadAsStringAsync();

        throw new Exception($"Status: {response.StatusCode}, Content: '{content}'");
    }
}
