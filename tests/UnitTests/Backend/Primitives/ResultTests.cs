namespace UnitTests.Backend.Primitives;

using BuildingBlocks.Domain.Primitives;
using FluentAssertions;

/// <summary>
/// Unit tests for <see cref="Result"/> and <see cref="Result{TValue}"/>.
/// </summary>
public sealed class ResultTests
{
    private sealed class TestResult(bool isSuccess, Error error) : Result(isSuccess, error);

    /// <summary>
    /// Verifies that Result.Success creates a successful result with Error.None.
    /// </summary>
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    /// <summary>
    /// Verifies that Result.Failure creates a failed result with the provided error.
    /// </summary>
    [Fact]
    public void Failure_ShouldCreateFailedResultWithError()
    {
        // Arrange
        var error = Error.Validation("Entity.Invalid", "Validation failed.");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    /// <summary>
    /// Verifies that attempting to create a successful result with an error throws an ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WhenSuccessWithNonNoneError_ShouldThrowArgumentException()
    {
        // Arrange
        var error = Error.Validation("Entity.Invalid", "Validation failed.");

        // Act
        var act = () => new TestResult(true, error);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Successful result cannot contain an error.*");
    }

    /// <summary>
    /// Verifies that attempting to create a failure result with Error.None throws an ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WhenFailureWithErrorNone_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new TestResult(false, Error.None);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Failed result must contain an error.*");
    }

    /// <summary>
    /// Verifies that Result&lt;T&gt;.Success returns a success outcome with the given value.
    /// </summary>
    [Fact]
    public void TypedSuccess_ShouldContainValueAndBeSuccessful()
    {
        // Arrange
        const string payload = "test-value";

        // Act
        var result = Result<string>.Success(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(payload);
        result.Error.Should().Be(Error.None);
    }

    /// <summary>
    /// Verifies that Result&lt;T&gt;.Failure returns a failure outcome with the given error.
    /// </summary>
    [Fact]
    public void TypedFailure_ShouldContainErrorAndBeFailure()
    {
        // Arrange
        var error = Error.NotFound("User.NotFound", "User not found.");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    /// <summary>
    /// Verifies that accessing the Value property of a failed result throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public void TypedFailure_WhenAccessingValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var error = Error.NotFound("User.NotFound", "User not found.");
        var result = Result<string>.Failure(error);

        // Act
        var act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value when result is a failure.");
    }

    /// <summary>
    /// Verifies implicit conversion from a payload value to a successful Result&lt;T&gt;.
    /// </summary>
    [Fact]
    public void ImplicitOperator_FromValue_ShouldReturnSuccessfulResult()
    {
        // Arrange
        const int payload = 42;

        // Act
        Result<int> result = payload;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().Be(Error.None);
    }

    /// <summary>
    /// Verifies implicit conversion from an Error to a failed Result&lt;T&gt;.
    /// </summary>
    [Fact]
    public void ImplicitOperator_FromError_ShouldReturnFailedResult()
    {
        // Arrange
        var error = Error.Unauthorized("Auth.Denied", "Access denied.");

        // Act
        Result<string> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    /// <summary>
    /// Verifies that Result.Create returns Success when the supplied value is not null.
    /// </summary>
    [Fact]
    public void Create_WhenValueIsNotNull_ShouldReturnSuccess()
    {
        // Arrange
        const string value = "hello";

        // Act
        var result = Result.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    /// <summary>
    /// Verifies that Result.Create returns Failure with Error.NullValue when the supplied value is null.
    /// </summary>
    [Fact]
    public void Create_WhenValueIsNull_ShouldReturnFailureWithNullValueError()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Result.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    /// <summary>
    /// Verifies that Match with Func executes the success branch when the result is successful.
    /// </summary>
    [Fact]
    public void Match_Func_WhenSuccess_ShouldInvokeOnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var output = result.Match(
            onSuccess: val => $"Value is {val}",
            onFailure: err => $"Error: {err.Code}");

        // Assert
        output.Should().Be("Value is 10");
    }

    /// <summary>
    /// Verifies that Match with Func executes the failure branch when the result is a failure.
    /// </summary>
    [Fact]
    public void Match_Func_WhenFailure_ShouldInvokeOnFailure()
    {
        // Arrange
        var result = Result<int>.Failure(Error.NotFound("Item.NotFound", "Not found"));

        // Act
        var output = result.Match(
            onSuccess: val => $"Value is {val}",
            onFailure: err => $"Error: {err.Code}");

        // Assert
        output.Should().Be("Error: Item.NotFound");
    }

    /// <summary>
    /// Verifies that Match with Action executes the success action when the result is successful.
    /// </summary>
    [Fact]
    public void Match_Action_WhenSuccess_ShouldExecuteOnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(100);
        var successExecuted = false;
        var failureExecuted = false;

        // Act
        result.Match(
            onSuccess: _ => successExecuted = true,
            onFailure: _ => failureExecuted = true);

        // Assert
        successExecuted.Should().BeTrue();
        failureExecuted.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Match with Action executes the failure action when the result is a failure.
    /// </summary>
    [Fact]
    public void Match_Action_WhenFailure_ShouldExecuteOnFailure()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Conflict("Item.Conflict", "Conflict"));
        var successExecuted = false;
        var failureExecuted = false;

        // Act
        result.Match(
            onSuccess: _ => successExecuted = true,
            onFailure: _ => failureExecuted = true);

        // Assert
        successExecuted.Should().BeFalse();
        failureExecuted.Should().BeTrue();
    }
}
