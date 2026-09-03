using FluentAssertions;
using Master.Application.Integrations.Repositories;
using Master.Domain.Integrations;
using Master.Infrastructure.Integrations;
using NSubstitute;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Unit tests for <see cref="InMemoryApiQuotaTracker"/>.
/// Validates thread-safe in-memory rate tracking across Meta, Google, TikTok, and Bing.
/// </summary>
public sealed class InMemoryApiQuotaTrackerTests
{
    private readonly IApiQuotaRepository _repository = Substitute.For<IApiQuotaRepository>();

    /// <summary>
    /// Initializes test context and resets shared trackers for isolation.
    /// </summary>
    public InMemoryApiQuotaTrackerTests()
    {
        InMemoryApiQuotaTracker.ResetAll();
    }

    /// <summary>
    /// Verifies that all 4 platforms are initialized by default.
    /// </summary>
    [Fact]
    public async Task GetAllQuotaStatusesAsync_ShouldReturnDefaultTrackersForAllFourPlatforms()
    {
        // Arrange
        var trackerService = new InMemoryApiQuotaTracker(_repository);

        // Act
        var statuses = await trackerService.GetAllQuotaStatusesAsync();

        // Assert
        statuses.Should().HaveCount(4);
        statuses.Select(s => s.Platform).Should().BeEquivalentTo(new[]
        {
            AdPlatform.Meta,
            AdPlatform.Google,
            AdPlatform.TikTok,
            AdPlatform.Bing
        });
    }

    /// <summary>
    /// Verifies that recording usage accumulates correctly and sets warning alert when 80% is reached.
    /// </summary>
    [Fact]
    public async Task RecordUsageAsync_ShouldAccumulateConsumptionAndTriggerWarningWhen80PercentReached()
    {
        // Arrange
        var trackerService = new InMemoryApiQuotaTracker(_repository);
        var now = DateTime.UtcNow;

        // Act 1: 50% consumption
        var result1 = await trackerService.RecordUsageAsync(AdPlatform.Meta, 50000, now);
        result1.IsSuccess.Should().BeTrue();
        result1.Value.UsagePercentage.Should().Be(50.0);
        result1.Value.IsWarning.Should().BeFalse();
        result1.Value.AlertLevel.Should().Be(QuotaAlertLevel.Normal);

        // Act 2: +30,000 -> 80,000 of 100,000 = 80.0%
        var result2 = await trackerService.RecordUsageAsync(AdPlatform.Meta, 30000, now);
        result2.IsSuccess.Should().BeTrue();
        result2.Value.UsagePercentage.Should().Be(80.0);
        result2.Value.IsWarning.Should().BeTrue();
        result2.Value.AlertLevel.Should().Be(QuotaAlertLevel.Warning);
    }

    /// <summary>
    /// Verifies that the tracker resets the window automatically once the duration has elapsed.
    /// </summary>
    [Fact]
    public async Task RecordUsageAsync_ShouldAutoResetWindow_WhenWindowDurationHasPassed()
    {
        // Arrange
        var trackerService = new InMemoryApiQuotaTracker(_repository);
        var start = DateTime.UtcNow.AddHours(-2); // 2 hours ago (window is 1 hour)

        await trackerService.RecordUsageAsync(AdPlatform.Meta, 85000, start);

        // Act: new call at current time (after window expired)
        var now = DateTime.UtcNow;
        var result = await trackerService.RecordUsageAsync(AdPlatform.Meta, 100, now);

        // Assert: window was reset, consumption is now just 100
        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentConsumption.Should().Be(100);
        result.Value.IsWarning.Should().BeFalse();
        result.Value.AlertLevel.Should().Be(QuotaAlertLevel.Normal);
    }
}
