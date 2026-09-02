namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Represents a typed domain or application error.
/// </summary>
/// <param name="Code">Stable machine-readable error code.</param>
/// <param name="Description">Human-readable error description.</param>
/// <param name="Type">Semantic classification of the error.</param>
public sealed record Error(string Code, string Description, ErrorType Type = ErrorType.Failure)
{
    /// <summary>
    /// Represents the absence of an error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>
    /// Represents an error indicating a required value was null.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "The specified value is null.", ErrorType.Failure);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A validation error instance.</returns>
    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A not found error instance.</returns>
    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A conflict error instance.</returns>
    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>An unauthorized error instance.</returns>
    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A forbidden error instance.</returns>
    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    /// <summary>
    /// Creates a generic failure error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A generic failure error instance.</returns>
    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);
}