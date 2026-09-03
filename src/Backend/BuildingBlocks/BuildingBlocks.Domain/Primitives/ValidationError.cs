namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Represents a structured validation error containing detailed property-level error messages.
/// </summary>
public sealed record ValidationError : Error
{
    private const string DefaultErrorCode = "Validation.General";
    private const string DefaultErrorDescription = "One or more validation failures occurred.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> record with detailed property errors.
    /// </summary>
    /// <param name="errors">Dictionary of property names and their associated error messages.</param>
    /// <param name="code">Machine-readable error code. Defaults to 'Validation.General'.</param>
    /// <param name="description">Human-readable summary description.</param>
    public ValidationError(
        IReadOnlyDictionary<string, string[]> errors,
        string code = DefaultErrorCode,
        string? description = null)
        : base(
            code,
            description ?? (errors.Count > 0
                ? string.Join(" ", errors.SelectMany(kvp => kvp.Value))
                : DefaultErrorDescription),
            ErrorType.Validation)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    /// <summary>
    /// Gets the property-level validation errors grouped by property name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Creates a <see cref="ValidationError"/> from a collection of property-level failures.
    /// </summary>
    /// <param name="failures">Collection of property name and error message tuples.</param>
    /// <param name="code">Machine-readable error code.</param>
    /// <returns>A new instance of <see cref="ValidationError"/>.</returns>
    public static ValidationError Create(
        IEnumerable<(string PropertyName, string ErrorMessage)> failures,
        string code = DefaultErrorCode)
    {
        ArgumentNullException.ThrowIfNull(failures);

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).Distinct().ToArray());

        return new ValidationError(errors, code);
    }
}
