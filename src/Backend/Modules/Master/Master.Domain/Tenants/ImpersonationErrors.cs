using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Tenants;

/// <summary>
/// Domain error catalog for tenant impersonation operations.
/// </summary>
public static class ImpersonationErrors
{
    /// <summary>
    /// Error returned when the target tenant is not active and therefore cannot be impersonated.
    /// </summary>
    public static readonly Error TenantInactive = Error.Failure(
        "Impersonation.TenantInactive",
        "Cannot impersonate a tenant that is suspended or inactive.");

    /// <summary>
    /// Error returned when a support ticket identifier is missing or formatted incorrectly.
    /// </summary>
    public static readonly Error InvalidTicket = Error.Validation(
        "Impersonation.InvalidTicket",
        "A valid support ticket identifier is mandatory for impersonation.");

    /// <summary>
    /// Error returned when the reason for impersonation does not meet minimum auditing length requirements.
    /// </summary>
    public static readonly Error InvalidReason = Error.Validation(
        "Impersonation.InvalidReason",
        "Impersonation reason must be specified and contain at least 10 characters.");

    /// <summary>
    /// Error returned when an impersonation session has expired.
    /// </summary>
    public static readonly Error SessionExpired = Error.Failure(
        "Impersonation.SessionExpired",
        "The requested impersonation session has expired.");

    /// <summary>
    /// Error returned when an impersonation session has already been revoked.
    /// </summary>
    public static readonly Error SessionRevoked = Error.Failure(
        "Impersonation.SessionRevoked",
        "The requested impersonation session has been revoked.");

    /// <summary>
    /// Error returned when an impersonation session was not found.
    /// </summary>
    public static readonly Error SessionNotFound = Error.NotFound(
        "Impersonation.SessionNotFound",
        "The requested impersonation session was not found.");
}
