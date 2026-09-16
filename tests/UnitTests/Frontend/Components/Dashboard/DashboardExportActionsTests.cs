using Analytics.Application.Dashboard.DTOs;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Dashboard;
using WebApp.Models;
using Xunit;

namespace UnitTests.Frontend.Components.Dashboard;

/// <summary>
/// Testes unitários com bUnit para o componente de exportação rápida (<see cref="DashboardExportActions"/>).
/// Valida geração do arquivo CSV estruturado, acionamento do download via JS e trigger de captura de imagem.
/// </summary>
public sealed class DashboardExportActionsTests : BunitTestBase
{
    private static ExecutiveDashboardDto CreateSampleDashboard()
    {
        var start = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        return new ExecutiveDashboardDto(
            start,
            end,
            start.AddDays(-7),
            start.AddDays(-1),
            "BRL",
            new List<ExecutiveMetricItemDto>
            {
                new("Spend", "Investimento Total", 1000m, 800m, 25.0m, true, "Currency"),
                new("Cpc", "Custo por Clique (CPC)", 1.50m, 2.00m, -25.0m, true, "Currency")
            },
            new List<ExecutiveTimeSeriesPointDto>
            {
                new(start, 500m, 2000m, 4.0m, 300, 10000, 25)
            },
            new List<PlatformShareDto>
            {
                new("Meta", 1000m, 4000m, 4.0m, 100m, 600, 50)
            },
            new List<DeviceShareDto>
            {
                new("Mobile", 1000m, 600, 50, 4.0m, 100m)
            });
    }

    /// <summary>
    /// Valida que quando o dashboard for nulo os botões de exportação permanecem desabilitados.
    /// </summary>
    [Fact]
    public void DashboardExportActions_WhenDashboardNull_ButtonsAreDisabled()
    {
        // Act
        var cut = Render<DashboardExportActions>(parameters => parameters
            .Add(p => p.Dashboard, (ExecutiveDashboardDto?)null));

        // Assert
        var csvBtn = cut.Find("#btn-export-csv");
        csvBtn.HasAttribute("disabled").Should().BeTrue();

        var imgBtn = cut.Find("#btn-export-image");
        imgBtn.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Valida que quando o dashboard está presente os botões de exportação ficam habilitados.
    /// </summary>
    [Fact]
    public void DashboardExportActions_WhenDashboardProvided_ButtonsAreEnabled()
    {
        // Arrange
        var dashboard = CreateSampleDashboard();

        // Act
        var cut = Render<DashboardExportActions>(parameters => parameters
            .Add(p => p.Dashboard, dashboard));

        // Assert
        var csvBtn = cut.Find("#btn-export-csv");
        csvBtn.HasAttribute("disabled").Should().BeFalse();

        var imgBtn = cut.Find("#btn-export-image");
        imgBtn.HasAttribute("disabled").Should().BeFalse();
    }

    /// <summary>
    /// Valida que a função BuildCsvContent gera o formato CSV completo com cabeçalhos e todas as 4 seções de dados.
    /// </summary>
    [Fact]
    public void DashboardExportActions_BuildCsvContent_ContainsAllSectionsAndFormattedData()
    {
        // Arrange
        var dashboard = CreateSampleDashboard();

        // Act
        var csv = DashboardExportActions.BuildCsvContent(dashboard);

        // Assert
        csv.Should().Contain("AdMetricsPro - Relatório Executivo Consolidado");
        csv.Should().Contain("METRICAS PRINCIPAIS");
        csv.Should().Contain("Investimento Total");
        csv.Should().Contain("Custo por Clique (CPC)");
        csv.Should().Contain("EVOLUCAO DIARIA");
        csv.Should().Contain("PARTICIPACAO POR CANAL");
        csv.Should().Contain("Meta;1000,00");
        csv.Should().Contain("PARTICIPACAO POR DISPOSITIVO");
        csv.Should().Contain("Mobile;1000,00");
    }

    /// <summary>
    /// Valida que o clique no botão de exportar CSV dispara a invocação do JSInterop correspondente.
    /// </summary>
    [Fact]
    public async Task DashboardExportActions_WhenExportCsvClicked_InvokesJsDownload()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        var dashboard = CreateSampleDashboard();

        var cut = Render<DashboardExportActions>(parameters => parameters
            .Add(p => p.Dashboard, dashboard));

        // Act
        var csvBtn = cut.Find("#btn-export-csv");
        await cut.InvokeAsync(() => csvBtn.Click());

        // Assert
        var invocation = JSInterop.VerifyInvoke("dashboardExport.downloadCsv");
        invocation.Arguments.Should().HaveCount(2);
        invocation.Arguments[0]?.ToString().Should().StartWith("dashboard_metricas_");
        invocation.Arguments[1]?.ToString().Should().Contain("AdMetricsPro - Relatório Executivo Consolidado");
    }

    /// <summary>
    /// Valida que o clique no botão de exportar imagem dispara a invocação do JSInterop para captura.
    /// </summary>
    [Fact]
    public async Task DashboardExportActions_WhenExportImageClicked_InvokesJsExport()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        var dashboard = CreateSampleDashboard();

        var cut = Render<DashboardExportActions>(parameters => parameters
            .Add(p => p.Dashboard, dashboard));

        // Act
        var imgBtn = cut.Find("#btn-export-image");
        await cut.InvokeAsync(() => imgBtn.Click());

        // Assert
        var invocation = JSInterop.VerifyInvoke("dashboardExport.exportImageOrPrint");
        invocation.Arguments.Should().HaveCount(1);
        invocation.Arguments[0]?.ToString().Should().Be("executive-dashboard-container");
    }
}
