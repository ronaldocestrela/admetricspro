using System.Text.Json.Serialization;

namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Represents the outcome of an operation without a return payload.
/// </summary>
[JsonConverter(typeof(ResultJsonConverterFactory))]
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">True when operation succeeded.</param>
    /// <param name="error">Associated error when operation fails.</param>
    /// <exception cref="ArgumentException">Thrown when success has an error or failure has no error.</exception>
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
    /// Creates a successful result without a payload.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">Failure details.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a successful typed result containing a payload.
    /// </summary>
    /// <typeparam name="TValue">Payload type.</typeparam>
    /// <param name="value">Returned payload.</param>
    /// <returns>A successful typed result containing <paramref name="value"/>.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>
    /// Creates a failed typed result with the specified error.
    /// </summary>
    /// <typeparam name="TValue">Payload type.</typeparam>
    /// <param name="error">Failure details.</param>
    /// <returns>A failed typed result.</returns>
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);

    /// <summary>
    /// Creates a typed result from a nullable value, returning success when present or failure with NullValue error.
    /// </summary>
    /// <typeparam name="TValue">Payload type.</typeparam>
    /// <param name="value">The value to evaluate.</param>
    /// <returns>A successful result if not null; otherwise a failure with <see cref="Error.NullValue"/>.</returns>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}

/// <summary>
/// Represents the outcome of an operation with a return payload.
/// </summary>
/// <typeparam name="TValue">Payload type.</typeparam>
[JsonConverter(typeof(ResultJsonConverterFactory))]
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

    /// <summary>
    /// Implicitly converts a value to a successful <see cref="Result{TValue}"/>, or failure if null.
    /// </summary>
    /// <param name="value">Payload value.</param>
    public static implicit operator Result<TValue>(TValue? value) => Create(value);

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> to a failed <see cref="Result{TValue}"/>.
    /// </summary>
    /// <param name="error">The error details.</param>
    public static implicit operator Result<TValue>(Error error) => Failure(error);

    /// <summary>
    /// Matches the result and executes the appropriate projection function based on outcome.
    /// </summary>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="onSuccess">Function invoked when the result is successful.</param>
    /// <param name="onFailure">Function invoked when the result is a failure.</param>
    /// <returns>The projected output value.</returns>
    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    /// <summary>
    /// Matches the result and executes the appropriate action based on outcome.
    /// </summary>
    /// <param name="onSuccess">Action invoked with the value when the result is successful.</param>
    /// <param name="onFailure">Action invoked with the error when the result is a failure.</param>
    public void Match(Action<TValue> onSuccess, Action<Error> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        if (IsSuccess)
        {
            onSuccess(Value);
        }
        else
        {
            onFailure(Error);
        }
    }
}