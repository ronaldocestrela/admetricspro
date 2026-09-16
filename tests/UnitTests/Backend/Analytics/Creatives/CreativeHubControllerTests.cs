using Analytics.Application.Creatives.DTOs;
using Analytics.Application.Creatives.Queries.GetCreativeFatigueAnalysis;
using Analytics.Application.Creatives.Queries.GetCreativeHubOverview;
using Analytics.Application.Creatives.Queries.GetCrossPlatformComparison;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using Xunit;

namespace UnitTests.Backend.Analytics.Creatives;

/// <summary>
/// Testes unitários para o controlador <see cref="CreativeHubController"/> (Subfase 5.2).
/// </summary>
public sealed class CreativeHubControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CreativeHubController _controller;

    /// <summary>
    /// Inicializa a suíte instanciando o controlador com o mediador mockado.
    /// </summary>
    public CreativeHubControllerTests()
    {
        _controller = new CreativeHubController(_sender);
    }

    /// <summary>
    /// Valida que GetOverview retorna Ok com payload quando a consulta tem sucesso.
    /// </summary>
    [Fact]
    public async Task GetOverview_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var overviewDto = new CreativeHubOverviewDto(
            workspaceId,
            TotalCreatives: 10,
            FatiguedCreativesCount: 2,
            WarningCreativesCount: 3,
            HealthyCreativesCount: 5,
            ReplacementsSuggestedCount: 2,
            Creatives: Array.Empty<CreativeFatigueDto>());

        _sender.Send(Arg.Any<GetCreativeHubOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreativeHubOverviewDto>.Success(overviewDto));

        // Act
        var actionResult = await _controller.GetOverview(workspaceId, null, null, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var result = okResult!.Value as Result<CreativeHubOverviewDto>;
        result!.IsSuccess.Should().BeTrue();
        result.Value.TotalCreatives.Should().Be(10);
    }

    /// <summary>
    /// Valida que GetFatigueAnalysis retorna BadRequest quando o mediador retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task GetFatigueAnalysis_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        _sender.Send(Arg.Any<GetCreativeFatigueAnalysisQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreativeFatigueDto>.Failure(Error.Validation("Workspace.InvalidId", "ID inválido.")));

        // Act
        var actionResult = await _controller.GetFatigueAnalysis(Guid.Empty, Guid.NewGuid(), null, null, CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
    }

    /// <summary>
    /// Valida que GetCrossPlatformComparison retorna Ok quando o comparador encontra métricas.
    /// </summary>
    [Fact]
    public async Task GetCrossPlatformComparison_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var metaDto = new CrossPlatformMetricsDto("MetaAds", 1, 1000m, 50000, 1500, 50m, 5000m, 3.0m, 0.66m, 20m, 5.0m);
        var tikTokDto = new CrossPlatformMetricsDto("TikTokAds", 1, 1000m, 60000, 1200, 25m, 2500m, 2.0m, 0.83m, 40m, 2.5m);

        var comparisonDto = new CrossPlatformComparisonDto(
            "hash_teaser",
            "Vídeo Teaser",
            null,
            metaDto,
            tikTokDto,
            "MetaAds",
            -50m,
            50m,
            "MetaAds foi mais eficiente");

        _sender.Send(Arg.Any<GetCrossPlatformComparisonQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CrossPlatformComparisonDto>.Success(comparisonDto));

        // Act
        var actionResult = await _controller.GetCrossPlatformComparison(workspaceId, "hash_teaser", "Vídeo Teaser", null, null, null, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var result = okResult!.Value as Result<CrossPlatformComparisonDto>;
        result!.IsSuccess.Should().BeTrue();
        result.Value.WinningPlatform.Should().Be("MetaAds");
    }
}
