using FluentAssertions;
using Master.Domain.Integrations;
using Master.Domain.Integrations.Events;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Unit tests for the <see cref="ApiQuotaTracker"/> aggregate root.
/// Validates quota consumption calculations, window management,
/// and threshold alerting (specifically the mandatory 80% early warning threshold).
/// </summary>
public sealed class ApiQuotaTrackerTests
{
    /// <summary>
    /// Verifies that creating a tracker with zero or negative max limit fails.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_ShouldFail_WhenMaxLimitIsZeroOrNegative(long invalidLimit)
    {
        // Act
        var result = ApiQuotaTracker.Create(
            AdPlatform.Meta,
            maxLimit: invalidLimit,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ApiQuota.InvalidMaxLimit");
    }

    /// <summary>
    /// Verifies that invalid warning threshold percentage fails validation.
    /// </summary>
    [Theory]
    [InlineData(-5.0)]
    [InlineData(0.0)]
    [InlineData(101.0)]
    public void Create_ShouldFail_WhenWarningThresholdIsOutOfRange(double invalidThreshold)
    {
        // Act
        var result = ApiQuotaTracker.Create(
            AdPlatform.Google,
            maxLimit: 10000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: invalidThreshold);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ApiQuota.InvalidThreshold");
    }

    /// <summary>
    /// Verifies that valid creation parameters initialize the tracker in Normal state with 0 consumption.
    /// </summary>
    [Fact]
    public void Create_ShouldSucceed_WithValidParameters()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var result = ApiQuotaTracker.Create(
            AdPlatform.Meta,
            maxLimit: 10000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0,
            windowStartUtc: now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tracker = result.Value;
        tracker.Platform.Should().Be(AdPlatform.Meta);
        tracker.MaxLimit.Should().Be(10000);
        tracker.CurrentConsumption.Should().Be(0);
        tracker.UsagePercentage.Should().Be(0.0);
        tracker.AlertLevel.Should().Be(QuotaAlertLevel.Normal);
        tracker.WarningThresholdPercentage.Should().Be(80.0);
        tracker.WindowDuration.Should().Be(TimeSpan.FromHours(1));
        tracker.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that consumption below 80% keeps alert level as Normal and raises no warning events.
    /// </summary>
    [Fact]
    public void RecordUsage_ShouldKeepNormalStatus_WhenConsumptionBelow80Percent()
    {
        // Arrange
        var tracker = ApiQuotaTracker.Create(
            AdPlatform.TikTok,
            maxLimit: 1000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0).Value;

        // Act: 799 / 1000 = 79.9%
        var result = tracker.RecordUsage(799, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tracker.CurrentConsumption.Should().Be(799);
        tracker.UsagePercentage.Should().BeApproximately(79.9, 0.01);
        tracker.AlertLevel.Should().Be(QuotaAlertLevel.Normal);
        tracker.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that reaching or exceeding 80% consumption transitions state to Warning
    /// and raises the ApiQuotaThresholdWarningEvent.
    /// </summary>
    [Fact]
    public void RecordUsage_ShouldTriggerWarningAlertAndRaiseEvent_WhenConsumptionReachesOrExceeds80Percent()
    {
        // Arrange
        var tracker = ApiQuotaTracker.Create(
            AdPlatform.Meta,
            maxLimit: 1000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0).Value;

        // Act: 800 / 1000 = 80.0%
        var now = DateTime.UtcNow;
        var result = tracker.RecordUsage(800, now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tracker.CurrentConsumption.Should().Be(800);
        tracker.UsagePercentage.Should().Be(80.0);
        tracker.AlertLevel.Should().Be(QuotaAlertLevel.Warning);
        tracker.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ApiQuotaThresholdWarningEvent>()
            .Which.Should().Match<ApiQuotaThresholdWarningEvent>(e =>
                e.Platform == AdPlatform.Meta &&
                e.AlertLevel == QuotaAlertLevel.Warning &&
                e.CurrentConsumption == 800 &&
                e.MaxLimit == 1000 &&
                e.UsagePercentage == 80.0);
    }

    /// <summary>
    /// Verifies that reaching or exceeding 95% consumption transitions state to Critical.
    /// </summary>
    [Fact]
    public void RecordUsage_ShouldTriggerCriticalAlert_WhenConsumptionReachesOrExceeds95Percent()
    {
        // Arrange
        var tracker = ApiQuotaTracker.Create(
            AdPlatform.Google,
            maxLimit: 1000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0).Value;

        // Act: 950 / 1000 = 95.0%
        var result = tracker.RecordUsage(950, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tracker.CurrentConsumption.Should().Be(950);
        tracker.UsagePercentage.Should().Be(95.0);
        tracker.AlertLevel.Should().Be(QuotaAlertLevel.Critical);
        tracker.DomainEvents.Should().Contain(e =>
            e is ApiQuotaThresholdWarningEvent &&
            ((ApiQuotaThresholdWarningEvent)e).AlertLevel == QuotaAlertLevel.Critical);
    }

    /// <summary>
    /// Verifies that reaching or exceeding 100% consumption transitions state to Exceeded.
    /// </summary>
    [Fact]
    public void RecordUsage_ShouldTriggerExceededAlert_WhenConsumptionReachesOrExceeds100Percent()
    {
        // Arrange
        var tracker = ApiQuotaTracker.Create(
            AdPlatform.Bing,
            maxLimit: 1000,
            windowDuration: TimeSpan.FromHours(1),
            warningThresholdPercentage: 80.0).Value;

        // Act: 1050 / 1000 = 105.0%
        var result = tracker.RecordUsage(1050, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tracker.CurrentConsumption.Should().Be(1050);
        tracker.UsagePercentage.Should().Be(105.0);
        tracker.AlertLevel.Should().Be(QuotaAlertLevel.Exceeded);
    }

    /// <summary>
    /// Verifies that resetting the window sets consumption to zero, returns status to Normal, and clears events.
    /// </summary>
    [Fact]
    public void ResetWindow_ShouldResetConsumptionAndAlertLevelToNormal()
    {
        // Arrange
        var tracker = ApiQuotaTracker.Create(
            AdPlatform.Meta,
            maxLimit: 1000,
            windowDuration: TimeSpan.FromHours(1)).Value;

        tracker.RecordUsage(850, DateTime.UtcNow);
        tracker.AlertLevel.Should().Be(QuotaAlertLevel.Warning);

        // Act
        var newWindowStart = DateTime.UtcNow.AddHours(1);
        tracker.ResetWindow(newWindowStart);

        // Assert
        tracker.CurrentConsumption.Should().Be(0);
        tracker.UsagePercentage.Should().Be(0.0);
        tracker.AlertLevel.Should().Be(QuotaAlertLevel.Normal);
        tracker.WindowStartUtc.Should().Be(newWindowStart);
    }
}
