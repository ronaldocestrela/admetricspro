namespace UnitTests.Backend.Primitives;

using BuildingBlocks.Domain.Primitives;
using FluentAssertions;

/// <summary>
/// Unit tests for <see cref="Error"/> and <see cref="ErrorType"/>.
/// </summary>
public sealed class ErrorTests
{
    /// <summary>
    /// Verifies that Error.None is initialized with empty strings and failure error type.
    /// </summary>
    [Fact]
    public void ErrorNone_ShouldHaveEmptyValuesAndFailureType()
    {
        // Act
        var error = Error.None;

        // Assert
        error.Code.Should().BeEmpty();
        error.Description.Should().BeEmpty();
        error.Type.Should().Be(ErrorType.Failure);
    }

    /// <summary>
    /// Verifies that Error.NullValue is initialized with standard code, description and failure type.
    /// </summary>
    [Fact]
    public void NullValue_ShouldHaveExpectedDefaults()
    {
        // Act
        var error = Error.NullValue;

        // Assert
        error.Code.Should().Be("Error.NullValue");
        error.Description.Should().Be("The specified value is null.");
        error.Type.Should().Be(ErrorType.Failure);
    }

    /// <summary>
    /// Verifies that Error.Validation creates an error with Validation type.
    /// </summary>
    [Fact]
    public void Validation_ShouldCreateErrorWithValidationType()
    {
        // Arrange
        const string code = "User.InvalidEmail";
        const string description = "Email is not in a valid format.";

        // Act
        var error = Error.Validation(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Verifies that Error.NotFound creates an error with NotFound type.
    /// </summary>
    [Fact]
    public void NotFound_ShouldCreateErrorWithNotFoundType()
    {
        // Arrange
        const string code = "Order.NotFound";
        const string description = "Order was not found.";

        // Act
        var error = Error.NotFound(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.NotFound);
    }

    /// <summary>
    /// Verifies that Error.Conflict creates an error with Conflict type.
    /// </summary>
    [Fact]
    public void Conflict_ShouldCreateErrorWithConflictType()
    {
        // Arrange
        const string code = "Tenant.SubdomainAlreadyExists";
        const string description = "The subdomain already exists.";

        // Act
        var error = Error.Conflict(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Conflict);
    }

    /// <summary>
    /// Verifies that Error.Unauthorized creates an error with Unauthorized type.
    /// </summary>
    [Fact]
    public void Unauthorized_ShouldCreateErrorWithUnauthorizedType()
    {
        // Arrange
        const string code = "Auth.Unauthorized";
        const string description = "User is not authorized.";

        // Act
        var error = Error.Unauthorized(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    /// <summary>
    /// Verifies that Error.Forbidden creates an error with Forbidden type.
    /// </summary>
    [Fact]
    public void Forbidden_ShouldCreateErrorWithForbiddenType()
    {
        // Arrange
        const string code = "Role.Forbidden";
        const string description = "You do not have permission to perform this action.";

        // Act
        var error = Error.Forbidden(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
        error.Type.Should().Be(ErrorType.Forbidden);
    }

    /// <summary>
    /// Verifies value equality semantics of Error records.
    /// </summary>
    [Fact]
    public void ErrorsWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var error1 = Error.Validation("Code", "Desc");
        var error2 = Error.Validation("Code", "Desc");

        // Assert
        error1.Should().Be(error2);
        (error1 == error2).Should().BeTrue();
    }
}
