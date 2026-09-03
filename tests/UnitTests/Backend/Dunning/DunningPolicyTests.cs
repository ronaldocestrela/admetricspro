using FluentAssertions;
using Master.Domain.Tenants;

namespace UnitTests.Backend.Dunning;

/// <summary>
/// Unit tests for <see cref="DunningPolicy"/> validating progressive suspension rules.
/// </summary>
public sealed class DunningPolicyTests
{
    private readonly DateTime _referenceUtc = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Verifies that when tenant has no due date or payment is not overdue, stage is None and all features are allowed.
    /// </summary>
    [Fact]
    public void EvaluateStage_ShouldReturnNone_WhenNotOverdueOrNoDueDate()
    {
        // Act
        var stageNoDueDate = DunningPolicy.EvaluateStage(null, _referenceUtc);
        var stageFutureDue = DunningPolicy.EvaluateStage(_referenceUtc.AddDays(1), _referenceUtc);

        // Assert
        stageNoDueDate.Should().Be(DunningStage.None);
        stageFutureDue.Should().Be(DunningStage.None);

        DunningPolicy.AreAutomationsAllowed(DunningStage.None).Should().BeTrue();
        DunningPolicy.AreReportsAllowed(DunningStage.None).Should().BeTrue();
        DunningPolicy.IsLoginAllowed(DunningStage.None).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that D+0 to D+2 is grace period (stage None, no restrictions).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EvaluateStage_ShouldReturnNone_DuringGracePeriod(int daysAgo)
    {
        // Arrange
        var dueDateUtc = _referenceUtc.AddDays(-daysAgo);

        // Act
        var stage = DunningPolicy.EvaluateStage(dueDateUtc, _referenceUtc);

        // Assert
        stage.Should().Be(DunningStage.None);
        DunningPolicy.AreAutomationsAllowed(stage).Should().BeTrue();
        DunningPolicy.AreReportsAllowed(stage).Should().BeTrue();
        DunningPolicy.IsLoginAllowed(stage).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that D+3 to D+6 is AutomationsDisabled stage.
    /// Automations are disabled, while reports and login remain allowed.
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void EvaluateStage_ShouldReturnAutomationsDisabled_WhenOverdueBetween3And6Days(int daysAgo)
    {
        // Arrange
        var dueDateUtc = _referenceUtc.AddDays(-daysAgo);

        // Act
        var stage = DunningPolicy.EvaluateStage(dueDateUtc, _referenceUtc);

        // Assert
        stage.Should().Be(DunningStage.AutomationsDisabled);
        DunningPolicy.AreAutomationsAllowed(stage).Should().BeFalse();
        DunningPolicy.AreReportsAllowed(stage).Should().BeTrue();
        DunningPolicy.IsLoginAllowed(stage).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that D+7 to D+13 is ReportsBlocked stage.
    /// Automations and reports are blocked, while login remains allowed.
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(10)]
    [InlineData(13)]
    public void EvaluateStage_ShouldReturnReportsBlocked_WhenOverdueBetween7And13Days(int daysAgo)
    {
        // Arrange
        var dueDateUtc = _referenceUtc.AddDays(-daysAgo);

        // Act
        var stage = DunningPolicy.EvaluateStage(dueDateUtc, _referenceUtc);

        // Assert
        stage.Should().Be(DunningStage.ReportsBlocked);
        DunningPolicy.AreAutomationsAllowed(stage).Should().BeFalse();
        DunningPolicy.AreReportsAllowed(stage).Should().BeFalse();
        DunningPolicy.IsLoginAllowed(stage).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that D+14 or more is LoginBlocked stage.
    /// Automations, reports and login are all blocked (total suspension).
    /// </summary>
    [Theory]
    [InlineData(14)]
    [InlineData(20)]
    [InlineData(60)]
    public void EvaluateStage_ShouldReturnLoginBlocked_WhenOverdue14DaysOrMore(int daysAgo)
    {
        // Arrange
        var dueDateUtc = _referenceUtc.AddDays(-daysAgo);

        // Act
        var stage = DunningPolicy.EvaluateStage(dueDateUtc, _referenceUtc);

        // Assert
        stage.Should().Be(DunningStage.LoginBlocked);
        DunningPolicy.AreAutomationsAllowed(stage).Should().BeFalse();
        DunningPolicy.AreReportsAllowed(stage).Should().BeFalse();
        DunningPolicy.IsLoginAllowed(stage).Should().BeFalse();
    }
}
