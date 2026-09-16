using System.Net;
using System.Text.Json;
using Analytics.Application.Creatives.DTOs;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using WebApp.Services.Creatives;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para o cliente HTTP do Creative Hub <see cref="CreativeHubClientService"/>.
/// </summary>
public sealed class CreativeHubClientServiceTests
{
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte configurando o tenant contextual mockado.
    /// </summary>
    public CreativeHubClientServiceTests()
    {
        _tenantStateProvider.CurrentTenant.Returns(new TenantState(_tenantId, "Tenant Test", "tenant-test", null, TenantBranding.Default));
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    /// <summary>
    /// Valida que GetOverviewAsync envia header X-Tenant-Id e desserializa o envelope Result.
    /// </summary>
    [Fact]
    public async Task GetOverviewAsync_ShouldSendTenantHeader_AndReturnOverviewDto()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var overviewDto = new CreativeHubOverviewDto(
            workspaceId,
            TotalCreatives: 5,
            FatiguedCreativesCount: 1,
            WarningCreativesCount: 1,
            HealthyCreativesCount: 3,
            ReplacementsSuggestedCount: 1,
            Creatives: Array.Empty<CreativeFatigueDto>());

        var expectedResult = Result<CreativeHubOverviewDto>.Success(overviewDto);
        string? capturedTenantHeader = null;

        var handler = new MockHttpMessageHandler(request =>
        {
            if (request.Headers.TryGetValues("X-Tenant-Id", out var values))
            {
                capturedTenantHeader = values.FirstOrDefault();
            }

            var json = JsonSerializer.Serialize(expectedResult);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.admetricspro.internal") };
        var service = new CreativeHubClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetOverviewAsync(workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCreatives.Should().Be(5);
        capturedTenantHeader.Should().Be(_tenantId.ToString());
    }

    /// <summary>
    /// Valida que GetOverviewAsync retorna falha quando o workspace informado é vazio.
    /// </summary>
    [Fact]
    public async Task GetOverviewAsync_ShouldFail_WhenWorkspaceIdIsEmpty()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
        {
            BaseAddress = new Uri("https://api.admetricspro.internal")
        };
        var service = new CreativeHubClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetOverviewAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.InvalidId");
    }
}
