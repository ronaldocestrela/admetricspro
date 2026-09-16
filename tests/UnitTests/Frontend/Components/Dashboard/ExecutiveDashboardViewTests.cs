using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Dashboard;
using WebApp.Models;
using Xunit;

namespace UnitTests.Frontend.Components.Dashboard;

/// <summary>
/// Testes unitários com bUnit para o container executivo integrado (<see cref="ExecutiveDashboardView"/>).
/// Valida carregamento inicial, filtros globais, renderização dos gráficos interativos, deltas e tratamento de falhas.
/// </summary>
public sealed class ExecutiveDashboardViewTests : BunitTestBase
{
    /// <summary>
    /// Valida que a inicialização renderiza controles de filtros, cartões de métricas, layout de gráficos e ações de exportação.
    /// </summary>
    [Fact]
    public void ExecutiveDashboardView_OnInitialized_RendersFiltersControlsMetricsAndCharts()
    {
        // Act
        var cut = Render<ExecutiveDashboardView>();

        // Assert
        var container = cut.Find("#executive-dashboard-container");
        container.Should().NotBeNull();

        var filtersBar = cut.Find(".dashboard-filters-bar");
        filtersBar.Should().NotBeNull();

        var metricsGrid = cut.Find(".metrics-grid");
        metricsGrid.Should().NotBeNull();

        var trendChart = cut.Find(".chart-col-trend");
        trendChart.Should().NotBeNull();

        var donutChart = cut.Find(".chart-col-donut");
        donutChart.Should().NotBeNull();

        var deviceChart = cut.Find(".dashboard-device-layout");
        deviceChart.Should().NotBeNull();

        var exportActions = cut.Find(".dashboard-export-actions");
        exportActions.Should().NotBeNull();
    }

    /// <summary>
    /// Valida que a mudança de canal ou período dispara uma nova requisição ao cliente analítico.
    /// </summary>
    [Fact]
    public async Task ExecutiveDashboardView_WhenFilterChanged_TriggersReload()
    {
        // Arrange
        var cut = Render<ExecutiveDashboardView>();

        // Act
        var platformSelect = cut.Find("#filter-platform");
        platformSelect.Change("Meta");

        // Assert
        await AnalyticsDashboardClientService.Received()
            .GetExecutiveDashboardAsync(
                Arg.Is<DashboardFiltersState>(f => f.Platform == "Meta"),
                Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao ocorrer uma falha na API, um alerta de erro amigável é exibido ao usuário.
    /// </summary>
    [Fact]
    public void ExecutiveDashboardView_WhenApiFails_RendersErrorAlert()
    {
        // Arrange
        AnalyticsDashboardClientService.GetExecutiveDashboardAsync(
            Arg.Any<DashboardFiltersState>(),
            Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Analytics.Application.Dashboard.DTOs.ExecutiveDashboardDto>.Failure(
                BuildingBlocks.Domain.Primitives.Error.Failure("Analytics.Unavailable", "Serviço de métricas indisponível no momento.")));

        // Act
        var cut = Render<ExecutiveDashboardView>();

        // Assert
        var errorAlert = cut.Find(".dashboard-alert-error");
        errorAlert.Should().NotBeNull();
        errorAlert.TextContent.Should().Contain("Serviço de métricas indisponível no momento.");
    }
}
