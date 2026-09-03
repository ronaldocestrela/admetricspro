using FluentAssertions;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Queries.GetApiHealthOverview;
using Master.Application.Integrations.Repositories;
using Master.Application.Integrations.Services;
using Master.Domain.Integrations;
using NSubstitute;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Unit tests for <see cref="GetApiHealthOverviewQueryHandler"/>.
/// </summary>
public sealed class GetApiHealthOverviewQueryHandlerTests
{
    private readonly IApiQuotaTrackerService _quotaService = Substitute.For<IApiQuotaTrackerService>();
    private readonly ITenantApiConnectionRepository _connectionRepo = Substitute.For<ITenantApiConnectionRepository>();
    private readonly GetApiHealthOverviewQueryHandler _handler;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public GetApiHealthOverviewQueryHandlerTests()
    {
        _handler = new GetApiHealthOverviewQueryHandler(_quotaService, _connectionRepo);
    }

    /// <summary>
    /// Verifies that the overview handler aggregates quota statuses and tenant connection counts.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnConsolidatedOverview()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var platformQuotas = new List<PlatformQuotaStatusDto>
        {
            new(AdPlatform.Meta, "Meta Graph API", 100000, 82000, 82.0, QuotaAlertLevel.Warning, true, TimeSpan.FromHours(1), now, now),
            new(AdPlatform.Google, "Google Ads API", 500000, 100000, 20.0, QuotaAlertLevel.Normal, false, TimeSpan.FromHours(24), now, now)
        };

        _quotaService.GetAllQuotaStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(platformQuotas);

        _connectionRepo.GetTotalCountAsync(Arg.Any<CancellationToken>()).Returns(100);
        _connectionRepo.CountByStatusAsync(ApiConnectionStatus.Connected, Arg.Any<CancellationToken>()).Returns(85);
        _connectionRepo.CountByStatusAsync(ApiConnectionStatus.ExpiringSoon, Arg.Any<CancellationToken>()).Returns(8);
        _connectionRepo.CountByStatusAsync(ApiConnectionStatus.Expired, Arg.Any<CancellationToken>()).Returns(5);
        _connectionRepo.CountByStatusAsync(ApiConnectionStatus.Revoked, Arg.Any<CancellationToken>()).Returns(1);
        _connectionRepo.CountByStatusAsync(ApiConnectionStatus.Disconnected, Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _handler.Handle(new GetApiHealthOverviewQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var overview = result.Value;
        overview.PlatformQuotas.Should().HaveCount(2);
        overview.TotalConnections.Should().Be(100);
        overview.ConnectedCount.Should().Be(85);
        overview.ExpiringSoonCount.Should().Be(8);
        overview.ExpiredCount.Should().Be(5);
        overview.RevokedOrDisconnectedCount.Should().Be(2);
    }
}
