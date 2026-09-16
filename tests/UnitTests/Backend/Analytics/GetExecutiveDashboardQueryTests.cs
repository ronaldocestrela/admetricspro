using Analytics.Application.Dashboard.DTOs;
using Analytics.Application.Dashboard.Queries.GetExecutiveDashboard;
using Analytics.Domain.Dashboard;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para a consulta CQRS <see cref="GetExecutiveDashboardQuery"/> e seu manipulador.
/// </summary>
public sealed class GetExecutiveDashboardQueryTests
{
    private readonly IExecutiveDashboardCalculator _calculatorMock = Substitute.For<IExecutiveDashboardCalculator>();
    private readonly IExecutiveDashboardDataProvider _dataProviderMock = Substitute.For<IExecutiveDashboardDataProvider>();
    private readonly GetExecutiveDashboardQueryHandler _handler;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetExecutiveDashboardQueryTests"/> com os mocks configurados.
    /// </summary>
    public GetExecutiveDashboardQueryTests()
    {
        _handler = new GetExecutiveDashboardQueryHandler(_calculatorMock, _dataProviderMock);
    }

    /// <summary>
    /// Valida que quando a data inicial é posterior à data final a query retorna falha de validação.
    /// </summary>
    [Fact]
    public async Task Handle_WhenStartDateIsAfterEndDate_ReturnsValidationFailure()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var query = new GetExecutiveDashboardQuery(
            WorkspaceId: Guid.NewGuid(),
            StartDateUtc: now,
            EndDateUtc: now.AddDays(-5));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GetExecutiveDashboard.InvalidDateRange");
    }

    /// <summary>
    /// Valida que quando os parâmetros são válidos a duração do período é calculada e delegada ao calculador.
    /// </summary>
    [Fact]
    public async Task Handle_WhenValid_CalculatesPreviousPeriodDurationAndDelegatesToCalculator()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var start = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc); // 6 days span
        var query = new GetExecutiveDashboardQuery(workspaceId, start, end, "Meta", "Mobile", "BRL");

        var domainResult = new ExecutiveDashboardResult(
            start,
            end,
            start.AddDays(-7),
            start.AddDays(-1),
            "BRL",
            new List<ExecutiveMetricItemResult>
            {
                new("Spend", "Investimento Total", 1000m, 800m, 25.0m, true, "Currency"),
                new("Cpc", "Custo por Clique (CPC)", 1.50m, 2.00m, -25.0m, true, "Currency"),
                new("Cpm", "Custo por Mil Impressões (CPM)", 15.0m, 18.0m, -16.67m, true, "Currency"),
                new("Ctr", "Taxa de Cliques (CTR)", 3.5m, 2.8m, 25.0m, true, "Percentage"),
                new("Cpa", "Custo por Aquisição (CPA)", 20.0m, 25.0m, -20.0m, true, "Currency"),
                new("Roas", "Retorno sobre Ad Spend (ROAS)", 4.0m, 3.2m, 25.0m, true, "Multiplier")
            },
            new List<ExecutiveTimeSeriesPointResult>
            {
                new(start, 500m, 2000m, 4.0m, 300, 10000, 25)
            },
            new List<PlatformShareResult>
            {
                new("Meta", 1000m, 4000m, 4.0m, 100m, 600, 50)
            },
            new List<DeviceShareResult>
            {
                new("Mobile", 1000m, 600, 50, 4.0m, 100m)
            });

        _calculatorMock.Calculate(
            Arg.Any<IEnumerable<RawMetricPoint>>(),
            Arg.Any<IEnumerable<RawMetricPoint>>(),
            start,
            end,
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            "BRL",
            "Meta",
            "Mobile")
            .Returns(Result<ExecutiveDashboardResult>.Success(domainResult));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Currency.Should().Be("BRL");
        dto.Metrics.Should().HaveCount(6);

        var spend = dto.Metrics.First(m => m.MetricKey == "Spend");
        spend.CurrentValue.Should().Be(1000m);
        spend.PreviousValue.Should().Be(800m);
        spend.PercentageChange.Should().Be(25.0m);

        var cpa = dto.Metrics.First(m => m.MetricKey == "Cpa");
        cpa.CurrentValue.Should().Be(20.0m);
        cpa.IsPositiveImprovement.Should().BeTrue();

        dto.PlatformBreakdown.Should().HaveCount(1);
        dto.DeviceBreakdown.Should().HaveCount(1);
    }
}
