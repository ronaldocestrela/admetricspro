using System.Net;
using System.Text.Json;
using Automations.Application.Pacing.DTOs;
using BuildingBlocks.Domain.Automations.Pacing;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para o cliente HTTP <see cref="BudgetPacingClientService"/>.
/// Valida envio de headers X-Tenant-Id, formatação de queries e desserialização de envelopes Result&lt;T&gt;.
/// </summary>
public sealed class BudgetPacingClientServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte configurando o estado do tenant.
    /// </summary>
    public BudgetPacingClientServiceTests()
    {
        var tenantState = new TenantState(_tenantId, "Agência Master", "master", null, TenantBranding.Default);
        _tenantStateProvider.CurrentTenant.Returns(tenantState);
    }

    /// <summary>
    /// Valida que a consulta de pacing de workspace chama a rota correta e anexa o header X-Tenant-Id.
    /// </summary>
    [Fact]
    public async Task GetWorkspacePacingAsync_WhenApiReturnsSuccess_ShouldReturnWorkspacePacingDto()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var expectedDto = new WorkspaceBudgetPacingDto
        {
            WorkspaceId = workspaceId,
            WorkspaceName = "Cliente Loja Virtual",
            TargetBudget = 10_000m,
            CurrentSpend = 5_000m,
            Status = PacingStatus.OnTrack
        };

        var apiResult = Result<WorkspaceBudgetPacingDto>.Success(expectedDto);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Contain($"/api/v1/automations/pacing/workspaces/{workspaceId}?year=2026&month=9");
            request.Headers.Contains("X-Tenant-Id").Should().BeTrue();
            request.Headers.GetValues("X-Tenant-Id").Should().Contain(_tenantId.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BudgetPacingClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetWorkspacePacingAsync(workspaceId, 2026, 9);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WorkspaceName.Should().Be("Cliente Loja Virtual");
        result.Value.TargetBudget.Should().Be(10_000m);
    }

    /// <summary>
    /// Valida que ao passar WorkspaceId vazio, a validação de guarda falha sem disparar requisição HTTP.
    /// </summary>
    [Fact]
    public async Task GetWorkspacePacingAsync_WithEmptyGuid_ShouldFailFast()
    {
        // Arrange
        var httpClient = new HttpClient(new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)))
        {
            BaseAddress = new Uri("https://localhost:7001")
        };
        var service = new BudgetPacingClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetWorkspacePacingAsync(Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.InvalidId");
    }

    /// <summary>
    /// Valida que a consulta da carteira (portfolio) invoca a rota portfolio e deserializa o sumário.
    /// </summary>
    [Fact]
    public async Task GetPortfolioPacingAsync_WhenApiReturnsSuccess_ShouldReturnSummaryDto()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var expectedSummary = new PortfolioPacingSummaryDto
        {
            TotalWorkspaces = 5,
            OnTrackCount = 3,
            OverCount = 1,
            UnderCount = 1,
            TotalContractedBudget = 50_000m,
            TotalCurrentSpend = 25_000m
        };

        var apiResult = Result<PortfolioPacingSummaryDto>.Success(expectedSummary);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Contain($"/api/v1/automations/pacing/portfolio?squadId={squadId}");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BudgetPacingClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetPortfolioPacingAsync(squadId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalWorkspaces.Should().Be(5);
        result.Value.OnTrackCount.Should().Be(3);
    }

    /// <summary>
    /// Valida a simulação de pacing dinâmico via HTTP POST.
    /// </summary>
    [Fact]
    public async Task SimulatePacingAsync_WhenApiReturnsSuccess_ShouldReturnSimulatedDto()
    {
        // Arrange
        var simRequest = new SimulatePacingRequestDto
        {
            TargetBudget = 20_000m,
            CurrentSpend = 12_000m,
            StartDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc),
            AsOfDateUtc = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        var expectedDto = new WorkspaceBudgetPacingDto
        {
            TargetBudget = 20_000m,
            CurrentSpend = 12_000m,
            Status = PacingStatus.Over
        };

        var apiResult = Result<WorkspaceBudgetPacingDto>.Success(expectedDto);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/automations/pacing/simulate");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BudgetPacingClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.SimulatePacingAsync(simRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PacingStatus.Over);
    }
}
