using Analytics.Application.Attribution.Dtos;
using Analytics.Application.Attribution.Queries.CalculateAttribution;
using Analytics.Application.Blended.Dtos;
using Analytics.Application.Blended.Queries.CalculateBlendedMetrics;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para os endpoints de Blended Metrics e Atribuição do AnalyticsController.
/// </summary>
public sealed class AnalyticsBlendedAndAttributionControllerTests
{
    private readonly ISender _senderMock = Substitute.For<ISender>();

    private AnalyticsController CreateController() => new(_senderMock);

    /// <summary>
    /// Valida que CalculateBlendedMetrics retorna 200 OK quando o handler retorna sucesso.
    /// </summary>
    [Fact]
    public async Task CalculateBlendedMetrics_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var dto = new BlendedMetricsDto(
            "BRL", 10000m, 30000m, 50000m, 100000, 2000, 100, 50, 5.0m, 3.0m, 200.0m, 100.0m, 5.0m, 100.0m, 2.0m,
            new List<BlendedChannelBreakdownDto>());

        _senderMock.Send(Arg.Any<CalculateBlendedMetricsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<BlendedMetricsDto>.Success(dto));

        var request = new CalculateBlendedMetricsApiRequest(
            new List<BlendedMetricItemApiRequest>
            {
                new("Meta", null, null, DateTime.UtcNow, 10000m, "BRL", 100000, 2000, 100, 30000m)
            },
            "BRL",
            50000m,
            50);

        // Act
        var actionResult = await controller.CalculateBlendedMetrics(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var value = okResult.Value as Result<BlendedMetricsDto>;
        value.Should().NotBeNull();
        value!.IsSuccess.Should().BeTrue();
        value.Value.MarketingEfficiencyRatio.Should().Be(5.0m);
    }

    /// <summary>
    /// Valida que CalculateBlendedMetrics retorna 400 BadRequest quando a requisição é nula ou o handler falha.
    /// </summary>
    [Fact]
    public async Task CalculateBlendedMetrics_WhenNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var actionResult = await controller.CalculateBlendedMetrics(null!, CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Valida que CalculateAttribution retorna 200 OK quando o processamento de jornadas tem sucesso.
    /// </summary>
    [Fact]
    public async Task CalculateAttribution_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var dto = new AttributionComparisonDto(
            new List<ChannelAttributionDto>(),
            new List<ChannelAttributionDto>(),
            new List<ChannelAttributionDto>(),
            1,
            1.0m,
            500m);

        _senderMock.Send(Arg.Any<CalculateAttributionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<AttributionComparisonDto>.Success(dto));

        var request = new CalculateAttributionApiRequest(
            new List<ConversionJourneyApiRequest>
            {
                new("j1", "c1", DateTime.UtcNow, 500m, new List<AttributionTouchpointApiRequest>
                {
                    new("Meta", "c1", DateTime.UtcNow.AddDays(-1), 1, 10m)
                })
            });

        // Act
        var actionResult = await controller.CalculateAttribution(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var value = okResult.Value as Result<AttributionComparisonDto>;
        value.Should().NotBeNull();
        value!.IsSuccess.Should().BeTrue();
        value.Value.TotalConversionValue.Should().Be(500m);
    }

    /// <summary>
    /// Valida que CalculateAttribution retorna 400 BadRequest quando a requisição é nula.
    /// </summary>
    [Fact]
    public async Task CalculateAttribution_WhenNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var actionResult = await controller.CalculateAttribution(null!, CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }
}
