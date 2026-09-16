using System.Net;
using System.Text.Json;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Campaigns.DTOs;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para o cliente HTTP <see cref="BulkCampaignClientService"/>.
/// Valida injeção de headers multitenant, chamadas às rotas da Web API e desserialização de envelopes Result&lt;T&gt;.
/// </summary>
public sealed class BulkCampaignClientServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BulkCampaignClientServiceTests"/> configurando o tenant contextual.
    /// </summary>
    public BulkCampaignClientServiceTests()
    {
        var tenantState = new TenantState(_tenantId, "Agência Multimídia", "multimidia", null, TenantBranding.Default);
        _tenantStateProvider.CurrentTenant.Returns(tenantState);
    }

    /// <summary>
    /// Valida que a consulta de campanhas para lote envia a rota e headers corretos e retorna a coleção.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task GetCampaignsForBulkAsync_WhenApiReturnsSuccess_ShouldReturnCampaignsList()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var expectedList = new List<CampaignHierarchyDto>
        {
            new(
                Guid.NewGuid(),
                workspaceId,
                Guid.NewGuid(),
                "MetaAds",
                "ext-1",
                "Campanha Black Friday",
                "Active",
                "Conversions",
                150m,
                null,
                "BRL",
                DateTime.UtcNow,
                null,
                DateTime.UtcNow,
                Array.Empty<AdSetHierarchyDto>())
        };

        var apiResult = Result<IReadOnlyList<CampaignHierarchyDto>>.Success(expectedList);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Contain($"/api/v1/integrations/campaigns?workspaceId={workspaceId}&platform=MetaAds");
            request.Headers.Contains("X-Tenant-Id").Should().BeTrue();
            request.Headers.GetValues("X-Tenant-Id").Should().Contain(_tenantId.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var service = new BulkCampaignClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetCampaignsForBulkAsync(workspaceId, "MetaAds");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Campanha Black Friday");
    }

    /// <summary>
    /// Valida que a execução de lote via POST /api/v1/integrations/campaigns/bulk serializa corretamente e retorna o resultado consolidado.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task ExecuteBulkOperationsAsync_WhenApiReturnsSuccess_ShouldReturnResultDto()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var camp1Id = Guid.NewGuid();
        var operations = new List<BulkCampaignOperationItem>
        {
            new(camp1Id, BulkCampaignActionType.Pause, Reason: "Pausa rápida")
        };

        var expectedResult = new BulkCampaignOperationResultDto
        {
            TotalRequested = 1,
            TotalSucceeded = 1,
            TotalFailed = 0,
            SucceededItems = new List<BulkOperationSuccessItemDto>
            {
                new(camp1Id, "Campanha 1", "MetaAds", BulkCampaignActionType.Pause, CampaignStatus.Active, CampaignStatus.Paused, 100m, 100m)
            }
        };

        var apiResult = Result<BulkCampaignOperationResultDto>.Success(expectedResult);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/integrations/campaigns/bulk");
            request.Headers.Contains("X-Tenant-Id").Should().BeTrue();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var service = new BulkCampaignClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.ExecuteBulkOperationsAsync(workspaceId, operations);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRequested.Should().Be(1);
        result.Value.TotalSucceeded.Should().Be(1);
        result.Value.SucceededItems.Should().HaveCount(1);
    }

    /// <summary>
    /// Valida que a execução com workspaceId vazio retorna erro de validação sem chamar a rede.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task ExecuteBulkOperationsAsync_WhenWorkspaceIdEmpty_ShouldReturnValidationFailure()
    {
        var httpClient = new HttpClient();
        var service = new BulkCampaignClientService(httpClient, _tenantStateProvider);

        var result = await service.ExecuteBulkOperationsAsync(Guid.Empty, new[]
        {
            new BulkCampaignOperationItem(Guid.NewGuid(), BulkCampaignActionType.Pause)
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.InvalidId");
    }

    /// <summary>
    /// Valida que a execução com lista de operações vazia retorna erro de validação sem chamar a rede.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task ExecuteBulkOperationsAsync_WhenOperationsEmpty_ShouldReturnValidationFailure()
    {
        var httpClient = new HttpClient();
        var service = new BulkCampaignClientService(httpClient, _tenantStateProvider);

        var result = await service.ExecuteBulkOperationsAsync(Guid.NewGuid(), Array.Empty<BulkCampaignOperationItem>());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BulkCampaign.EmptyOperations");
    }
}
