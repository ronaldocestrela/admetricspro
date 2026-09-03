namespace BuildingBlocks.Application.Security;

/// <summary>
/// Defines standardized claim types utilized during tenant impersonation (Shadow Mode) operations.
/// </summary>
public static class ImpersonationClaims
{
    /// <summary>
    /// Claim indicating whether the token/session is operating under active tenant impersonation.
    /// Value is either "true" or "false".
    /// </summary>
    public const string IsImpersonated = "is_impersonated";

    /// <summary>
    /// Claim holding the unique identifier of the SuperAdmin or support engineer performing the impersonation.
    /// </summary>
    public const string OriginalSuperAdminId = "original_superadmin_id";

    /// <summary>
    /// Claim identifying the tenant target of the impersonation session.
    /// </summary>
    public const string TenantId = "tenant_id";

    /// <summary>
    /// Claim containing the mandatory support ticket identifier justifying this impersonation session.
    /// </summary>
    public const string SupportTicketId = "support_ticket";

    /// <summary>
    /// Claim containing the unique identifier of the persisted impersonation session.
    /// </summary>
    public const string SessionId = "impersonation_session_id";
}
