using Analytics.Application.Attribution.Dtos;
using Analytics.Application.Attribution.Queries.CalculateAttribution;
using Analytics.Application.Blended.Dtos;
using Analytics.Application.Blended.Queries.CalculateBlendedMetrics;
using Analytics.Domain.Attribution;
using Analytics.Domain.Blended;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para as consultas e manipuladores CQRS de Blended Metrics e Atribuição Multicanal.
/// </summary>
public sealed class AnalyticsBlendedAndAttributionQueriesTests
{
    private readonly IBlendedMetricsCalculator _blendedCalculatorMock = Substitute.For<IBlendedMetricsCalculator>();
    private readonly IAttributionCalculator _attributionCalculatorMock = Substitute.For<IAttributionCalculator>();

    /// <summary>
    /// Valida que CalculateBlendedMetricsQueryHandler delega ao calculador e mapeia para DTO com sucesso.
    /// </summary>
    [Fact]
    public async Task CalculateBlendedMetricsQueryHandler_WhenValid_ReturnsSuccessDto()
    {
        // Arrange
        var domainResult = new BlendedMetricsResult(
            "BRL",
            10000m,
            30000m,
            50000m,
            100000,
            2500,
            150,
            100,
            5.0m,
            3.0m,
            100.0m,
            66.67m,
            4.0m,
            100.0m,
            2.5m,
            new List<BlendedChannelBreakdown>
            {
                new("Meta", 6000m, 60.0m, 60000, 1500, 90, 18000m, 3.0m, 66.67m, 4.0m, 100.0m, 2.5m),
                new("Google", 4000m, 40.0m, 40000, 1000, 60, 12000m, 3.0m, 66.67m, 4.0m, 100.0m, 2.5m)
            });

        _blendedCalculatorMock.CalculateAsync(
            Arg.Any<IEnumerable<BlendedMetricInputItem>>(),
            "BRL",
            50000m,
            100,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BlendedMetricsResult>.Success(domainResult)));

        var handler = new CalculateBlendedMetricsQueryHandler(_blendedCalculatorMock);

        var query = new CalculateBlendedMetricsQuery(
            new List<BlendedMetricItemInput>
            {
                new("Meta", null, null, DateTime.UtcNow, 6000m, "BRL", 60000, 1500, 90, 18000m),
                new("Google", null, null, DateTime.UtcNow, 4000m, "BRL", 40000, 1000, 60, 12000m)
            },
            "BRL",
            50000m,
            100);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.TargetCurrency.Should().Be("BRL");
        dto.TotalSpend.Should().Be(10000m);
        dto.MarketingEfficiencyRatio.Should().Be(5.0m);
        dto.BlendedRoas.Should().Be(3.0m);
        dto.BlendedCac.Should().Be(100.0m);
        dto.ChannelBreakdowns.Should().HaveCount(2);
    }

    /// <summary>
    /// Valida que CalculateBlendedMetricsQueryHandler propaga o erro retornado pelo calculador de domínio.
    /// </summary>
    [Fact]
    public async Task CalculateBlendedMetricsQueryHandler_WhenCalculatorFails_ReturnsFailure()
    {
        // Arrange
        _blendedCalculatorMock.CalculateAsync(
            Arg.Any<IEnumerable<BlendedMetricInputItem>>(),
            Arg.Any<string>(),
            Arg.Any<decimal?>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BlendedMetricsResult>.Failure(
                Error.Validation("BlendedMetrics.InvalidCurrency", "Moeda inválida"))));

        var handler = new CalculateBlendedMetricsQueryHandler(_blendedCalculatorMock);
        var query = new CalculateBlendedMetricsQuery(new List<BlendedMetricItemInput>(), "INVALID");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BlendedMetrics.InvalidCurrency");
    }

    /// <summary>
    /// Valida que CalculateAttributionQueryHandler delega ao calculador e mapeia os modelos comparativos para DTO.
    /// </summary>
    [Fact]
    public async Task CalculateAttributionQueryHandler_WhenValid_ReturnsSuccessComparisonDto()
    {
        // Arrange
        var domainComparison = new AttributionComparisonResult(
            new List<ChannelAttributionResult>
            {
                new("Meta", 1.0m, 500m, 1, 1, 0, 0, 5.0m, 100m)
            },
            new List<ChannelAttributionResult>
            {
                new("Google", 1.0m, 500m, 1, 0, 1, 0, 5.0m, 100m)
            },
            new List<ChannelAttributionResult>
            {
                new("Meta", 0.5m, 250m, 1, 1, 0, 0, 2.5m, 200m),
                new("Google", 0.5m, 250m, 1, 0, 1, 0, 2.5m, 200m)
            },
            1,
            1.0m,
            500m);

        _attributionCalculatorMock.CompareModels(
            Arg.Any<IEnumerable<ConversionJourney>>(),
            Arg.Any<IReadOnlyDictionary<string, decimal>?>())
            .Returns(Result<AttributionComparisonResult>.Success(domainComparison));

        var handler = new CalculateAttributionQueryHandler(_attributionCalculatorMock);

        var convDate = DateTime.UtcNow;
        var query = new CalculateAttributionQuery(
            new List<ConversionJourneyInput>
            {
                new("j1", "c1", convDate, 500m, new List<AttributionTouchpointInput>
                {
                    new("Meta", "c_top", convDate.AddDays(-2)),
                    new("Google", "c_bottom", convDate.AddHours(-1))
                })
            },
            null,
            new Dictionary<string, decimal> { ["Meta"] = 100m, ["Google"] = 100m });

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.TotalJourneys.Should().Be(1);
        dto.TotalConversions.Should().Be(1.0m);
        dto.TotalConversionValue.Should().Be(500m);
        dto.FirstTouchChannels.Should().HaveCount(1);
        dto.LastTouchChannels.Should().HaveCount(1);
        dto.LinearChannels.Should().HaveCount(2);
    }
}
