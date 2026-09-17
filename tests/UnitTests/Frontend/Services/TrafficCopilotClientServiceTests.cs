using System.Net;
using System.Text.Json;
using Analytics.Application.Copilot.DTOs;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using WebApp.Services.Copilot;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para o cliente HTTP do Copiloto de IA <see cref="TrafficCopilotClientService"/>.
/// </summary>
public sealed class TrafficCopilotClientServiceTests
{
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte de testes configurando o tenant contextual mockado.
    /// </summary>
    public TrafficCopilotClientServiceTests()
    {
        _tenantStateProvider.CurrentTenant.Returns(new TenantState(_tenantId, "Tenant Test", "tenant-test", null, WebApp.State.TenantBranding.Default));
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
    /// Valida que GetDailyDiagnosticAsync envia o cabeçalho X-Tenant-Id e desserializa o envelope Result com sucesso.
    /// </summary>
    [Fact]
    public async Task GetDailyDiagnosticAsync_ShouldSendTenantHeader_AndReturnDiagnosticReport()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var reportDto = new DailyDiagnosticReportDto(
            WorkspaceId: workspaceId,
            ReportDate: DateTime.UtcNow.Date,
            ExecutiveSummary: "Resumo Executivo",
            WinsSummary: "Vitórias",
            RisksSummary: "Riscos",
            AudienceOverlapAnomalies: Array.Empty<AudienceOverlapAnomalyDto>(),
            SearchCannibalizationAnomalies: Array.Empty<SearchCannibalizationAnomalyDto>(),
            Actions: Array.Empty<CopilotRecommendationActionDto>(),
            EstimatedMonthlySavings: 750m,
            CriticalAnomaliesCount: 0,
            HighAnomaliesCount: 0);

        string? capturedTenantHeader = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.Headers.TryGetValues("X-Tenant-Id", out var values))
            {
                capturedTenantHeader = values.FirstOrDefault();
            }

            var envelope = Result<DailyDiagnosticReportDto>.Success(reportDto);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web)))
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var service = new TrafficCopilotClientService(client, _tenantStateProvider);

        // Act
        var result = await service.GetDailyDiagnosticAsync(workspaceId);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.WorkspaceId.Should().Be(workspaceId);
        result.Value.EstimatedMonthlySavings.Should().Be(750m);
        capturedTenantHeader.Should().Be(_tenantId.ToString());
    }

    /// <summary>
    /// Valida que GetDailyDiagnosticAsync retorna falha quando o workspace for Guid.Empty.
    /// </summary>
    [Fact]
    public async Task GetDailyDiagnosticAsync_ShouldReturnValidationFailure_WhenWorkspaceIsEmpty()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var service = new TrafficCopilotClientService(client, _tenantStateProvider);

        // Act
        var result = await service.GetDailyDiagnosticAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.InvalidId");
    }

    /// <summary>
    /// Valida que ExecuteActionAsync despacha o comando via POST e retorna a confirmação de execução em 1 clique.
    /// </summary>
    [Fact]
    public async Task ExecuteActionAsync_ShouldPostPayload_AndReturnExecutionResult()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var actionDto = new CopilotRecommendationActionDto(
            ActionId: Guid.NewGuid(),
            ActionType: "PauseAdSet",
            TargetEntityId: Guid.NewGuid(),
            TargetEntityName: "Conjunto Redundante",
            Platform: "MetaAds",
            Title: "Pausar conjunto",
            Description: "Pausar conjunto de pior CPA",
            Parameters: new Dictionary<string, string>(),
            IsApplied: false,
            AppliedAtUtc: null);

        var resultDto = new ExecuteCopilotActionResultDto(
            ActionId: actionDto.ActionId,
            Success: true,
            Message: "Ação executada com sucesso em 1 clique.",
            ExecutedAtUtc: DateTime.UtcNow);

        HttpMethod? capturedMethod = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            capturedMethod = req.Method;
            var envelope = Result<ExecuteCopilotActionResultDto>.Success(resultDto);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web)))
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var service = new TrafficCopilotClientService(client, _tenantStateProvider);

        // Act
        var result = await service.ExecuteActionAsync(workspaceId, actionDto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        capturedMethod.Should().Be(HttpMethod.Post);
    }

    /// <summary>
    /// Valida que GetDailyDiagnosticAsync retorna falha graciosa e informativa quando a API responde sem conteúdo (corpo vazio).
    /// </summary>
    [Fact]
    public async Task GetDailyDiagnosticAsync_ShouldReturnFailure_WhenApiResponseIsEmpty()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(string.Empty)
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var service = new TrafficCopilotClientService(client, _tenantStateProvider);

        // Act
        var result = await service.GetDailyDiagnosticAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Http.EmptyResponse");
        result.Error.Description.Should().Contain("500");
    }

    /// <summary>
    /// Valida que GetDailyDiagnosticAsync retorna falha graciosa quando a API responde HTML ou JSON malformado.
    /// </summary>
    [Fact]
    public async Task GetDailyDiagnosticAsync_ShouldReturnFailure_WhenApiResponseIsInvalidJson()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html><body>502 Bad Gateway</body></html>")
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var service = new TrafficCopilotClientService(client, _tenantStateProvider);

        // Act
        var result = await service.GetDailyDiagnosticAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Http.InvalidJson");
    }

    /// <summary>
    /// Valida que ExecuteActionAsync retorna falha graciosa quando a API responde sem conteúdo.
    /// </summary>
    [Fact]
    public async Task ExecuteActionAsync_ShouldReturnFailure_WhenApiResponseIsEmpty()
    {
        // Arrange
        var actionDto = new CopilotRecommendationActionDto(
            ActionId: Guid.NewGuid(),
            ActionType: "PauseAdSet",
            TargetEntityId: Guid.NewGuid(),
            TargetEntityName: "Conjunto",
            Platform: "MetaAds",
            Title: "Pausar",
            Description: "Pausar conjunto",
            Parameters: new Dictionary<string, string>(),
            IsApplied: false,
            AppliedAtUtc: null);

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(string.Empty)
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test") };
        var service = new TrafficCopilotClientService(client, _tenantStateProvider);

        // Act
        var result = await service.ExecuteActionAsync(Guid.NewGuid(), actionDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Http.EmptyResponse");
        result.Error.Description.Should().Contain("404");
    }
}
