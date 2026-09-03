using FluentAssertions;
using Master.Application.Tenants.Commands.TerminateImpersonationSession;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TerminateImpersonationSessionCommandValidator"/>.
/// </summary>
public sealed class TerminateImpersonationSessionCommandValidatorTests
{
    private readonly TerminateImpersonationSessionCommandValidator _validator = new();

    /// <summary>
    /// Verifies that validation fails when TenantId or SessionId are empty.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenIdsAreEmpty()
    {
        // Arrange
        var command = new TerminateImpersonationSessionCommand(Guid.Empty, Guid.Empty, "Encerramento");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TerminateImpersonationSessionCommand.TenantId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(TerminateImpersonationSessionCommand.SessionId));
    }

    /// <summary>
    /// Verifies that validation passes when valid identifiers are provided.
    /// </summary>
    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new TerminateImpersonationSessionCommand(Guid.NewGuid(), Guid.NewGuid(), "Encerramento de chamado técnico");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
