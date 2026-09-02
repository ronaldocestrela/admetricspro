namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Represents the outcome of an operation without a return payload.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">True when operation succeeded.</param>
    /// <param name="error">Associated error when operation fails.</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the associated error for failed operations.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">Failure details.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// Represents the outcome of an operation with a return payload.
/// </summary>
/// <typeparam name="TValue">Payload type.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the payload for successful operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when operation failed.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value when result is a failure.");

    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    /// <param name="value">Returned payload.</param>
    /// <returns>A successful result containing <paramref name="value"/>.</returns>
    public static Result<TValue> Success(TValue value) => new(true, value, Error.None);

    /// <summary>
    /// Creates a failed typed result.
    /// </summary>
    /// <param name="error">Failure details.</param>
    /// <returns>A failed typed result.</returns>
    public static new Result<TValue> Failure(Error error) => new(false, default, error);
}