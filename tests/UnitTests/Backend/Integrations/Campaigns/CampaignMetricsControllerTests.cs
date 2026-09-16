using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Campaigns.Queries.GetCampaignMetrics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o controlador REST <see cref="CampaignMetricsController"/>.
/// </summary>
public sealed class CampaignMetricsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CampaignMetricsController _controller;

    /// <summary>
    /// Inicializa o controlador com o mediador mockado.
    /// </summary>
    public CampaignMetricsControllerTests()
    {
        _controller = new CampaignMetricsController(_sender);
    }

    /// <summary>
    /// Valida que requisição com payload nulo retorna HTTP 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task SyncMetrics_WhenRequestIsNull_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.SyncMetrics(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Valida que execução bem-sucedida retorna HTTP 200 OK com o resumo das métricas.
    /// </summary>
    [Fact]
    public async Task SyncMetrics_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var request = new SyncCampaignMetricsApiRequest(Guid.NewGuid());
        var summary = new SyncCampaignMetricsSummaryDto(
            TotalAccountsProcessed: 1,
            TotalRecordsIngested: 10,
            TotalSpend: 1500m,
            TotalImpressions: 50000,
            TotalClicks: 2500,
            TotalConversions: 80m,
            TotalConversionValue: 6400m,
            SyncedAtUtc: DateTime.UtcNow,
            Granularity: "Daily",
            SyncedPlatforms: new[] { "MetaAds" });

        _sender.Send(Arg.Any<SyncCampaignMetricsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<SyncCampaignMetricsSummaryDto>.Success(summary)));

        // Act
        var result = await _controller.SyncMetrics(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var val = okResult.Value.Should().BeOfType<Result<SyncCampaignMetricsSummaryDto>>().Subject;
        val.IsSuccess.Should().BeTrue();
        val.Value.TotalRecordsIngested.Should().Be(10);
        val.Value.TotalSpend.Should().Be(1500m);
    }

    /// <summary>
    /// Valida que erro NotFound no comando retorna HTTP 404.
    /// </summary>
    [Fact]
    public async Task SyncMetrics_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var request = new SyncCampaignMetricsApiRequest(Guid.NewGuid(), Guid.NewGuid());
        _sender.Send(Arg.Any<SyncCampaignMetricsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<SyncCampaignMetricsSummaryDto>.Failure(
                Error.NotFound("Sync.NotFound", "Conta não encontrada."))));

        // Act
        var result = await _controller.SyncMetrics(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Valida que consulta de métricas retorna HTTP 200 OK com a lista de métricas analíticas.
    /// </summary>
    [Fact]
    public async Task GetMetrics_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var dtos = new List<CampaignMetricDto>
        {
            new(
                Id: Guid.NewGuid(),
                WorkspaceId: workspaceId,
                ConnectedAdAccountId: Guid.NewGuid(),
                CampaignId: Guid.NewGuid(),
                AdSetId: null,
                AdId: null,
                Platform: "GoogleAds",
                ExternalCampaignId: "cmp_g1",
                ExternalAdSetId: null,
                ExternalAdId: null,
                Date: DateTime.UtcNow.Date,
                Hour: null,
                Granularity: "Daily",
                Spend: 300m,
                Currency: "BRL",
                Impressions: 10000,
                Clicks: 400,
                Conversions: 20m,
                ConversionValue: 1200m,
                Ctr: 4.0m,
                Cpc: 0.75m,
                Cpm: 30.0m,
                Cpa: 15.0m,
                Roas: 4.0m,
                SyncedAtUtc: DateTime.UtcNow)
        };

        _sender.Send(Arg.Any<GetCampaignMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CampaignMetricDto>>.Success(dtos)));

        // Act
        var result = await _controller.GetMetrics(workspaceId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var val = okResult.Value.Should().BeOfType<Result<IReadOnlyList<CampaignMetricDto>>>().Subject;
        val.IsSuccess.Should().BeTrue();
        val.Value.Should().HaveCount(1);
    }
}
