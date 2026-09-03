using Bunit;
using FluentAssertions;
using Master.Application.Integrations.DTOs;
using Master.Domain.Integrations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Backoffice;
using WebApp.Services;
using BuildingBlocks.Domain.Primitives;

namespace UnitTests.Frontend.Components.Backoffice;

/// <summary>
/// Unit tests with bUnit for the <see cref="ApiHealthDashboard"/> component.
/// Validates quota meter visual indicators (including 80% warning badge),
/// platform cards rendering, and tenant connection status grids.
/// </summary>
public sealed class ApiHealthDashboardTests : BunitTestBase
{
    private readonly IApiHealthClientService _apiHealthService = Substitute.For<IApiHealthClientService>();

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public ApiHealthDashboardTests()
    {
        Services.AddSingleton<IApiHealthClientService>(_apiHealthService);
    }

    private static ApiHealthOverviewDto CreateSampleOverview(bool metaInWarning = true)
    {
        var now = DateTime.UtcNow;
        var platformQuotas = new List<PlatformQuotaStatusDto>
        {
            new(AdPlatform.Meta, "Meta Graph API", 100000, metaInWarning ? 82450 : 40000, metaInWarning ? 82.45 : 40.0, metaInWarning ? QuotaAlertLevel.Warning : QuotaAlertLevel.Normal, metaInWarning, TimeSpan.FromHours(1), now, now),
            new(AdPlatform.Google, "Google Ads API", 500000, 150000, 30.0, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(24), now, now),
            new(AdPlatform.TikTok, "TikTok Marketing API", 60000, 10000, 16.67, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(1), now, now),
            new(AdPlatform.Bing, "Bing Ads API", 30000, 5000, 16.67, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(1), now, now)
        };

        return new ApiHealthOverviewDto(
            PlatformQuotas: platformQuotas,
            TotalConnections: 45,
            ConnectedCount: 40,
            ExpiringSoonCount: 3,
            ExpiredCount: 1,
            RevokedOrDisconnectedCount: 1,
            TimestampUtc: now);
    }

    private static List<TenantApiConnectionDto> CreateSampleConnections()
    {
        var now = DateTime.UtcNow;
        return new List<TenantApiConnectionDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Agência Delta", AdPlatform.Meta, "Meta Graph API", "act_111", "Conta Principal", ApiConnectionStatus.Connected, now.AddDays(30), now, null, now),
            new(Guid.NewGuid(), Guid.NewGuid(), "Beta E-commerce", AdPlatform.Google, "Google Ads API", "222-333", "Google Search", ApiConnectionStatus.ExpiringSoon, now.AddDays(3), now, "Expira em 3 dias", now),
            new(Guid.NewGuid(), Guid.NewGuid(), "Gamma Store", AdPlatform.TikTok, "TikTok Marketing API", "tt_444", "TikTok Ads", ApiConnectionStatus.Expired, now.AddDays(-1), now, "Token expirado", now)
        };
    }

    /// <summary>
    /// Verifies that all 4 platforms are rendered as quota cards.
    /// </summary>
    [Fact]
    public void Dashboard_ShouldRenderAllFourPlatformQuotaCards()
    {
        // Arrange
        _apiHealthService.GetOverviewAsync(Arg.Any<CancellationToken>())
            .Returns(Result<ApiHealthOverviewDto>.Success(CreateSampleOverview()));
        _apiHealthService.GetConnectionsAsync(Arg.Any<AdPlatform?>(), Arg.Any<ApiConnectionStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantApiConnectionDto>>.Success(CreateSampleConnections()));

        // Act
        var cut = Render<ApiHealthDashboard>();

        // Assert
        cut.FindAll(".quota-card").Should().HaveCount(4);
        cut.Markup.Should().Contain("Meta Graph API");
        cut.Markup.Should().Contain("Google Ads API");
        cut.Markup.Should().Contain("TikTok Marketing API");
        cut.Markup.Should().Contain("Bing Ads API");
    }

    /// <summary>
    /// Verifies that when a platform quota reaches 80% or more, a prominent warning badge is rendered.
    /// </summary>
    [Fact]
    public void Dashboard_ShouldRenderWarningBadge_WhenQuotaReaches80Percent()
    {
        // Arrange: Meta is at 82.45%
        _apiHealthService.GetOverviewAsync(Arg.Any<CancellationToken>())
            .Returns(Result<ApiHealthOverviewDto>.Success(CreateSampleOverview(metaInWarning: true)));
        _apiHealthService.GetConnectionsAsync(Arg.Any<AdPlatform?>(), Arg.Any<ApiConnectionStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantApiConnectionDto>>.Success(CreateSampleConnections()));

        // Act
        var cut = Render<ApiHealthDashboard>();

        // Assert
        var warningBadges = cut.FindAll(".badge-quota-warning");
        warningBadges.Should().NotBeEmpty();
        cut.Markup.Should().Contain("80%+");
    }

    /// <summary>
    /// Verifies that tenant connections table renders connection rows and status badges.
    /// </summary>
    [Fact]
    public void Dashboard_ShouldRenderTenantConnectionsTableWithStatusBadges()
    {
        // Arrange
        _apiHealthService.GetOverviewAsync(Arg.Any<CancellationToken>())
            .Returns(Result<ApiHealthOverviewDto>.Success(CreateSampleOverview()));
        _apiHealthService.GetConnectionsAsync(Arg.Any<AdPlatform?>(), Arg.Any<ApiConnectionStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TenantApiConnectionDto>>.Success(CreateSampleConnections()));

        // Act
        var cut = Render<ApiHealthDashboard>();

        // Assert
        cut.FindAll(".connection-row").Should().HaveCount(3);
        cut.Markup.Should().Contain("Agência Delta");
        cut.Markup.Should().Contain("Beta E-commerce");
        cut.Markup.Should().Contain("Gamma Store");
        cut.FindAll(".badge-status-connected").Should().NotBeEmpty();
        cut.FindAll(".badge-status-expiring").Should().NotBeEmpty();
        cut.FindAll(".badge-status-expired").Should().NotBeEmpty();
    }
}
