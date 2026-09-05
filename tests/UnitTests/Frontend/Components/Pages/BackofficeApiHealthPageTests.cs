using BackofficeApp.Components.Pages;
using BackofficeApp.Services;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Master.Application.Integrations.DTOs;
using Master.Domain.Integrations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes de componente bUnit para a página de Saúde das APIs &amp; Rate Limits (<see cref="ApiHealthPage"/>).
/// Valida a adesão ao padrão de design executivo do Backoffice: cabeçalho com categoria,
/// título, descrição, ações com botão primário e integração com o dashboard de telemetria.
/// </summary>
public sealed class BackofficeApiHealthPageTests : BunitTestBase
{
    private readonly IApiHealthClientService _apiHealthService;

    private static ApiHealthOverviewDto CreateSampleOverview()
    {
        var now = DateTime.UtcNow;
        var quotas = new List<PlatformQuotaStatusDto>
        {
            new(AdPlatform.Meta, "Meta Graph API", 100000, 45000, 45.0, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(1), now, now),
            new(AdPlatform.Google, "Google Ads API", 500000, 120000, 24.0, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(24), now, now),
            new(AdPlatform.TikTok, "TikTok Marketing API", 60000, 5000, 8.33, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(1), now, now),
            new(AdPlatform.Bing, "Bing Ads API", 30000, 2000, 6.67, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(1), now, now)
        };

        return new ApiHealthOverviewDto(
            PlatformQuotas: quotas,
            TotalConnections: 12,
            ConnectedCount: 11,
            ExpiringSoonCount: 1,
            ExpiredCount: 0,
            RevokedOrDisconnectedCount: 0,
            TimestampUtc: now);
    }

    private static List<TenantApiConnectionDto> CreateSampleConnections()
    {
        var now = DateTime.UtcNow;
        return
        [
            new(Guid.NewGuid(), Guid.NewGuid(), "Agência Horizon", AdPlatform.Meta, "Meta Graph API", "act_999", "Principal", ApiConnectionStatus.Connected, now.AddDays(45), now, null, now)
        ];
    }

    /// <summary>
    /// Inicializa os serviços necessários para os testes.
    /// </summary>
    public BackofficeApiHealthPageTests()
    {
        _apiHealthService = Substitute.For<IApiHealthClientService>();
        _apiHealthService.GetOverviewAsync(Arg.Any<CancellationToken>())
            .Returns(Result<ApiHealthOverviewDto>.Success(CreateSampleOverview()));
        _apiHealthService.GetConnectionsAsync(Arg.Any<AdPlatform?>(), Arg.Any<ApiConnectionStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantApiConnectionDto>>.Success(CreateSampleConnections()));

        Services.AddSingleton(_apiHealthService);
    }

    /// <summary>
    /// Valida que a página renderiza o cabeçalho executivo padronizado do Backoffice.
    /// </summary>
    [Fact]
    public void ApiHealthPage_OnInitialized_ShouldRenderStandardExecutiveHeader()
    {
        // Act
        var cut = Render<ApiHealthPage>();

        // Assert
        cut.Find(".page-header").Should().NotBeNull();
        cut.Find(".page-category").TextContent.Should().Contain("Backoffice Global");
        cut.Find(".page-title").TextContent.Should().Contain("Monitor de Integrações & Rate Limits");
        cut.Find(".page-description").TextContent.Should().Contain("Telemetria em tempo real");
        cut.Find(".header-actions button.btn-primary").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que o componente ApiHealthDashboard é renderizado dentro da página.
    /// </summary>
    [Fact]
    public void ApiHealthPage_ShouldRenderDashboardWithQuotasAndConnections()
    {
        // Act
        var cut = Render<ApiHealthPage>();

        // Assert
        cut.Find(".api-health-dashboard").Should().NotBeNull();
        cut.FindAll(".quota-card").Should().HaveCount(4);
        cut.Markup.Should().Contain("Meta Graph API");
        cut.Markup.Should().Contain("Agência Horizon");
    }

    /// <summary>
    /// Valida que o botão de ação no cabeçalho invoca a atualização de dados.
    /// </summary>
    [Fact]
    public void ApiHealthPage_WhenRefreshClicked_ShouldInvokeServiceReload()
    {
        // Arrange
        var cut = Render<ApiHealthPage>();

        // Act
        var refreshBtn = cut.Find(".header-actions button.btn-primary");
        refreshBtn.Click();

        // Assert
        _apiHealthService.Received().GetOverviewAsync(Arg.Any<CancellationToken>());
    }
}
