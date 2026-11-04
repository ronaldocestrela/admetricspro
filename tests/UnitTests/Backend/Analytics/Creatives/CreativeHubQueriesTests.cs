using Analytics.Application.Creatives.Queries.GetCreativeFatigueAnalysis;
using Analytics.Application.Creatives.Queries.GetCreativeHubOverview;
using Analytics.Application.Creatives.Queries.GetCrossPlatformComparison;
using Analytics.Domain.Creatives;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Analytics.Creatives;

/// <summary>
/// Testes unitários para os manipuladores de queries do Creative Hub e Detector de Fadiga (Subfase 5.2).
/// </summary>
public sealed class CreativeHubQueriesTests
{
    private readonly ICreativeHubDataProvider _dataProvider = Substitute.For<ICreativeHubDataProvider>();
    private readonly IAdFatigueDetector _fatigueDetector = new AdFatigueDetector();
    private readonly ICreativeHubAggregator _aggregator = new CreativeHubAggregator();
    private readonly ICreativeHubAssetRepository _assetRepository = Substitute.For<ICreativeHubAssetRepository>();

    /// <summary>
    /// Valida que a consulta de análise de fadiga rejeita identificadores vazios.
    /// </summary>
    [Fact]
    public async Task GetCreativeFatigueAnalysis_ShouldFail_WhenWorkspaceOrAdIdIsEmpty()
    {
        // Arrange
        var handler = new GetCreativeFatigueAnalysisQueryHandler(_dataProvider, _fatigueDetector);

        // Act & Assert
        var resultEmptyWorkspace = await handler.Handle(
            new GetCreativeFatigueAnalysisQuery(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        resultEmptyWorkspace.IsFailure.Should().BeTrue();
        resultEmptyWorkspace.Error.Code.Should().Be("Workspace.InvalidId");

        var resultEmptyAdId = await handler.Handle(
            new GetCreativeFatigueAnalysisQuery(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        resultEmptyAdId.IsFailure.Should().BeTrue();
        resultEmptyAdId.Error.Code.Should().Be("Ad.InvalidId");
    }

    /// <summary>
    /// Valida que a consulta de análise de fadiga retorna o diagnóstico completo quando as métricas são válidas.
    /// </summary>
    [Fact]
    public async Task GetCreativeFatigueAnalysis_ShouldReturnDto_WhenMetricsAreValid()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var adId = Guid.NewGuid();
        var handler = new GetCreativeFatigueAnalysisQueryHandler(_dataProvider, _fatigueDetector);

        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var metrics = new List<CreativeDailyMetricPoint>
        {
            new(startDate.AddDays(0), 10000, 300, 100m, 10m, 300m, 1.2m),
            new(startDate.AddDays(1), 10000, 250, 100m, 8m, 250m, 1.8m),
            new(startDate.AddDays(2), 10000, 200, 100m, 6m, 200m, 2.2m),
            new(startDate.AddDays(3), 10000, 150, 100m, 4m, 150m, 2.8m),
            new(startDate.AddDays(4), 10000, 120, 100m, 3m, 120m, 3.2m)
        };

        _dataProvider.GetAdDailyMetricsAsync(workspaceId, adId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeDailyMetricPoint>>.Success(metrics));

        _dataProvider.GetWorkspaceActiveAdsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeMetadata>>.Success(new List<CreativeMetadata>
            {
                new(adId, "Criativo Promoção", "MetaAds", "https://cdn.example.com/promo.mp4", "hash_promo")
            }));

        // Act
        var result = await handler.Handle(
            new GetCreativeFatigueAnalysisQuery(workspaceId, adId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdId.Should().Be(adId);
        result.Value.ReplacementSuggested.Should().BeTrue();
        result.Value.Status.Should().Be("Fatigued");
    }

    /// <summary>
    /// Valida que a consulta de comparação cross-platform retorna DTO com canais contrastados.
    /// </summary>
    [Fact]
    public async Task GetCrossPlatformComparison_ShouldReturnDto_WhenMetricsAreAvailable()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var handler = new GetCrossPlatformComparisonQueryHandler(_dataProvider, _aggregator);

        var date = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var metaMetrics = new List<CreativeDailyMetricPoint>
        {
            new(date, 50000, 1500, 1000m, 50m, 5000m, 1.5m)
        };
        var tikTokMetrics = new List<CreativeDailyMetricPoint>
        {
            new(date, 60000, 1200, 1000m, 25m, 2500m, 1.4m)
        };

        _dataProvider.GetCrossPlatformMetricsAsync(workspaceId, "hash_teaser", Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<(IReadOnlyList<CreativeDailyMetricPoint>, IReadOnlyList<CreativeDailyMetricPoint>)>.Success((metaMetrics, tikTokMetrics)));

        // Act
        var result = await handler.Handle(
            new GetCrossPlatformComparisonQuery(workspaceId, "hash_teaser", "Vídeo Teaser"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WinningPlatform.Should().Be("MetaAds");
        result.Value.MetaMetrics.Spend.Should().Be(1000m);
        result.Value.TikTokMetrics.Spend.Should().Be(1000m);
    }

    /// <summary>
    /// Valida que a visão geral do Creative Hub agrega contadores de criativos e fadiga corretamente.
    /// </summary>
    [Fact]
    public async Task GetCreativeHubOverview_ShouldAggregateOverviewCounts()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var adId1 = Guid.NewGuid();
        var adId2 = Guid.NewGuid();

        var handler = new GetCreativeHubOverviewQueryHandler(_dataProvider, _fatigueDetector);

        _dataProvider.GetWorkspaceActiveAdsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeMetadata>>.Success(new List<CreativeMetadata>
            {
                new(adId1, "Ad Fadigado", "MetaAds", null, "hash1"),
                new(adId2, "Ad Saudável", "TikTokAds", null, "hash2")
            }));

        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var fatiguedMetrics = new List<CreativeDailyMetricPoint>
        {
            new(startDate.AddDays(0), 10000, 300, 100m, 10m, 300m, 1.5m),
            new(startDate.AddDays(1), 10000, 250, 100m, 8m, 250m, 2.0m),
            new(startDate.AddDays(2), 10000, 180, 100m, 5m, 180m, 2.5m),
            new(startDate.AddDays(3), 10000, 120, 100m, 3m, 120m, 3.0m)
        };

        var healthyMetrics = new List<CreativeDailyMetricPoint>
        {
            new(startDate.AddDays(0), 10000, 200, 100m, 8m, 240m, 1.2m),
            new(startDate.AddDays(1), 10000, 205, 100m, 8m, 240m, 1.3m),
            new(startDate.AddDays(2), 10000, 198, 100m, 8m, 240m, 1.3m),
            new(startDate.AddDays(3), 10000, 202, 100m, 8m, 240m, 1.4m)
        };

        _dataProvider.GetAdDailyMetricsAsync(workspaceId, adId1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeDailyMetricPoint>>.Success(fatiguedMetrics));

        _dataProvider.GetAdDailyMetricsAsync(workspaceId, adId2, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeDailyMetricPoint>>.Success(healthyMetrics));

        // Act
        var result = await handler.Handle(
            new GetCreativeHubOverviewQuery(workspaceId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var overview = result.Value;

        overview.WorkspaceId.Should().Be(workspaceId);
        overview.TotalCreatives.Should().Be(2);
        overview.FatiguedCreativesCount.Should().Be(1);
        overview.HealthyCreativesCount.Should().Be(1);
        overview.ReplacementsSuggestedCount.Should().Be(1);
    }

    /// <summary>
    /// Visão geral inclui ativos orgânicos do repositório sem alterar contadores de fadiga pagos.
    /// </summary>
    [Fact]
    public async Task GetCreativeHubOverview_ShouldIncludeOrganicAssets_WhenRepositoryHasPermalink()
    {
        var workspaceId = Guid.NewGuid();
        var adId = Guid.NewGuid();
        var organicAssetId = Guid.NewGuid();
        var organicPostId = Guid.NewGuid();

        var handler = new GetCreativeHubOverviewQueryHandler(_dataProvider, _fatigueDetector, _assetRepository);

        _dataProvider.GetWorkspaceActiveAdsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeMetadata>>.Success(new List<CreativeMetadata>
            {
                new(adId, "Ad Fadigado", "MetaAds", null, "hash1"),
            }));

        var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var fatiguedMetrics = new List<CreativeDailyMetricPoint>
        {
            new(startDate.AddDays(0), 10000, 300, 100m, 10m, 300m, 1.5m),
            new(startDate.AddDays(1), 10000, 250, 100m, 8m, 250m, 2.0m),
            new(startDate.AddDays(2), 10000, 180, 100m, 5m, 180m, 2.5m),
            new(startDate.AddDays(3), 10000, 120, 100m, 3m, 120m, 3.0m),
        };

        _dataProvider.GetAdDailyMetricsAsync(workspaceId, adId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeDailyMetricPoint>>.Success(fatiguedMetrics));

        var organicEntity = new CreativeHubAsset(
            organicAssetId,
            workspaceId,
            "fingerprint-organic",
            organicPostId,
            "Instagram",
            "https://instagram.com/p/organic",
            5000,
            0.05m,
            0.02m,
            2.5m,
            DateTime.UtcNow);

        _assetRepository.ListOrganicByWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeHubAsset>>.Success(new List<CreativeHubAsset> { organicEntity }));

        var result = await handler.Handle(new GetCreativeHubOverviewQuery(workspaceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FatiguedCreativesCount.Should().Be(1);
        result.Value.HealthyCreativesCount.Should().Be(0);
        result.Value.OrganicAssets.Should().ContainSingle();
        result.Value.OrganicAssets![0].Permalink.Should().Be("https://instagram.com/p/organic");
        result.Value.OrganicAssets[0].Network.Should().Be("Instagram");
    }

    /// <summary>
    /// Sem repositório de ativos orgânicos a lista orgânica vem vazia.
    /// </summary>
    [Fact]
    public async Task GetCreativeHubOverview_ShouldReturnEmptyOrganicAssets_WhenRepositoryIsNull()
    {
        var workspaceId = Guid.NewGuid();
        var handler = new GetCreativeHubOverviewQueryHandler(_dataProvider, _fatigueDetector, assetRepository: null);

        _dataProvider.GetWorkspaceActiveAdsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CreativeMetadata>>.Success(Array.Empty<CreativeMetadata>()));

        var result = await handler.Handle(new GetCreativeHubOverviewQuery(workspaceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrganicAssets.Should().BeEmpty();
    }
}
