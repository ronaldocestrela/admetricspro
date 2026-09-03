using FluentAssertions;
using Master.Application.Tenants.Commands.ImpersonateTenant;
using Master.Domain.Tenants;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="ImpersonateTenantCommandValidator"/>.
/// </summary>
public sealed class ImpersonateTenantCommandValidatorTests
{
    private readonly ImpersonateTenantCommandValidator _validator = new();

    /// <summary>
    /// Verifies validation failure when support ticket is null, empty or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenSupportTicketIsEmpty(string? ticketId)
    {
        // Arrange
        var command = new ImpersonateTenantCommand(
            TenantId.New(),
            Guid.NewGuid(),
            ticketId!,
            "Investigação de divergência de métricas no Meta Ads",
            30);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.SupportTicketId));
    }

    /// <summary>
    /// Verifies validation failure when support ticket length is out of allowed range.
    /// </summary>
    [Theory]
    [InlineData("AB")] // Less than 3 chars
    public void Validate_ShouldFail_WhenSupportTicketIsTooShort(string ticketId)
    {
        // Arrange
        var command = new ImpersonateTenantCommand(
            TenantId.New(),
            Guid.NewGuid(),
            ticketId,
            "Investigação de divergência de métricas no Meta Ads",
            30);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.SupportTicketId));
    }

    /// <summary>
    /// Verifies validation failure when reason is null, empty or shorter than 10 characters.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Curto")]
    [InlineData("123456789")]
    public void Validate_ShouldFail_WhenReasonIsInvalidOrTooShort(string? reason)
    {
        // Arrange
        var command = new ImpersonateTenantCommand(
            TenantId.New(),
            Guid.NewGuid(),
            "INC-12345",
            reason!,
            30);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Reason));
    }

    /// <summary>
    /// Verifies validation failure when reason exceeds 500 characters.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenReasonExceeds500Characters()
    {
        // Arrange
        var longReason = new string('A', 501);
        var command = new ImpersonateTenantCommand(
            TenantId.New(),
            Guid.NewGuid(),
            "INC-12345",
            longReason,
            30);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Reason));
    }

    /// <summary>
    /// Verifies validation failure when TenantId is null.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenTenantIdIsNull()
    {
        // Arrange
        var command = new ImpersonateTenantCommand(
            null!,
            Guid.NewGuid(),
            "INC-12345",
            "Investigação de divergência de métricas no Meta Ads",
            30);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.TenantId));
    }

    /// <summary>
    /// Verifies validation failure when SuperAdminId is empty.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenSuperAdminIdIsEmpty()
    {
        // Arrange
        var command = new ImpersonateTenantCommand(
            TenantId.New(),
            Guid.Empty,
            "INC-12345",
            "Investigação de divergência de métricas no Meta Ads",
            30);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.SuperAdminId));
    }

    /// <summary>
    /// Verifies validation failure when duration is outside the range 5-120 minutes.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(121)]
    [InlineData(-10)]
    public void Validate_ShouldFail_WhenDurationIsOutOfRange(int durationMinutes)
    {
        // Arrange
        var command = new ImpersonateTenantCommand(
            TenantId.New(),
            Guid.NewGuid(),
            "INC-12345",
            "Investigação de divergência de métricas no Meta Ads",
            durationMinutes);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.DurationMinutes));
    }

    /// <summary>
    /// Verifies validation success when all parameters meet the domain rules.
    /// </summary>
    [Fact]
    public void Validate_ShouldSucceed_WhenCommandIsValid()
    {
        // Arrange
        var command = new ImpersonateTenantCommand(
            TenantId.New(),
            Guid.NewGuid(),
            "INC-12345",
            "Investigação de divergência de métricas no Meta Ads",
            45);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
