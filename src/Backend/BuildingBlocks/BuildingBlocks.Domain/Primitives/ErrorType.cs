namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Specifies the semantic category of an error.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Generic or unexpected failure.
    /// </summary>
    Failure = 0,

    /// <summary>
    /// Validation error indicating input did not meet criteria.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// Error indicating a requested resource or aggregate was not found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// Error indicating a conflict with the current state of a resource.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// Error indicating authentication credentials are missing or invalid.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// Error indicating authenticated user lacks permission for the action.
    /// </summary>
    Forbidden = 5
}
