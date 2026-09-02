namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Represents a typed domain/application error.
/// </summary>
/// <param name="Code">Stable machine-readable error code.</param>
/// <param name="Description">Human-readable error description.</param>
public sealed record Error(string Code, string Description)
{
    /// <summary>
    /// Represents the absence of an error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A validation error instance.</returns>
    public static Error Validation(string code, string description) =>
        new(code, description);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A not found error instance.</returns>
    public static Error NotFound(string code, string description) =>
        new(code, description);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>A conflict error instance.</returns>
    public static Error Conflict(string code, string description) =>
        new(code, description);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="description">Human-readable description.</param>
    /// <returns>An unauthorized error instance.</returns>
    public static Error Unauthorized(string code, string description) =>
        new(code, description);
}