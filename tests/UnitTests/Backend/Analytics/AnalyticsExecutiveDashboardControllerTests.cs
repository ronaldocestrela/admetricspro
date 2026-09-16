using Analytics.Application.Dashboard.DTOs;
using Analytics.Application.Dashboard.Queries.GetExecutiveDashboard;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para o endpoint executivo do AnalyticsController (<c>GET /api/v1/analytics/dashboard/executive</c>).
/// </summary>
public sealed class AnalyticsExecutiveDashboardControllerTests
{
    private readonly ISender _senderMock = Substitute.For<ISender>();

    /// <summary>
    /// Cria uma nova instância de <see cref="AnalyticsController"/> com o mediador mockado.
    /// </summary>
    private AnalyticsController CreateController() => new(_senderMock);

    /// <summary>
    /// Valida que GetExecutiveDashboard retorna 200 OK com o envelope Result quando o handler conclui com sucesso.
    /// </summary>
    [Fact]
    public async Task GetExecutiveDashboard_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var workspaceId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(-7);
        var end = DateTime.UtcNow;

        var dto = new ExecutiveDashboardDto(
            start,
            end,
            start.AddDays(-7),
            start.AddDays(-1),
            "BRL",
            new List<ExecutiveMetricItemDto>
            {
                new("Spend", "Investimento Total", 5000m, 4000m, 25.0m, true, "Currency"),
                new("Cpc", "Custo por Clique (CPC)", 1.25m, 1.50m, -16.67m, true, "Currency"),
                new("Cpm", "Custo por Mil Impressões (CPM)", 12.0m, 15.0m, -20.0m, true, "Currency"),
                new("Ctr", "Taxa de Cliques (CTR)", 3.2m, 2.8m, 14.29m, true, "Percentage"),
                new("Cpa", "Custo por Aquisição (CPA)", 25.0m, 30.0m, -16.67m, true, "Currency"),
                new("Roas", "Retorno sobre Ad Spend (ROAS)", 4.5m, 3.8m, 18.42m, true, "Multiplier")
            },
            new List<ExecutiveTimeSeriesPointDto>(),
            new List<PlatformShareDto>(),
            new List<DeviceShareDto>());

        _senderMock.Send(Arg.Any<GetExecutiveDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExecutiveDashboardDto>.Success(dto));

        // Act
        var actionResult = await controller.GetExecutiveDashboard(
            workspaceId,
            start,
            end,
            "Meta",
            "Mobile",
            "BRL",
            CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var value = okResult.Value as Result<ExecutiveDashboardDto>;
        value.Should().NotBeNull();
        value!.IsSuccess.Should().BeTrue();
        value.Value.Metrics.Should().HaveCount(6);
    }

    /// <summary>
    /// Valida que GetExecutiveDashboard retorna 400 BadRequest quando a query falha na validação.
    /// </summary>
    [Fact]
    public async Task GetExecutiveDashboard_WhenFailure_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        _senderMock.Send(Arg.Any<GetExecutiveDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExecutiveDashboardDto>.Failure(
                Error.Validation("GetExecutiveDashboard.InvalidDateRange", "Intervalo de datas inválido.")));

        // Act
        var actionResult = await controller.GetExecutiveDashboard(
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-5),
            null,
            null,
            null,
            CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
        var value = badRequestResult.Value as Result<ExecutiveDashboardDto>;
        value.Should().NotBeNull();
        value!.IsFailure.Should().BeTrue();
    }
}
