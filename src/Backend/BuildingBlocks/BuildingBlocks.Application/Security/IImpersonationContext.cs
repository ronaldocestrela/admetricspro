namespace BuildingBlocks.Application.Security;

/// <summary>
/// Provides contextual information about active impersonation (Shadow Mode) in the current execution scope.
/// </summary>
public interface IImpersonationContext
{
    /// <summary>
    /// Gets a value indicating whether the current execution context is running under tenant impersonation.
    /// </summary>
    bool IsImpersonated { get; }

    /// <summary>
    /// Gets the unique identifier of the SuperAdmin who initiated the impersonation, if active.
    /// </summary>
    Guid? OriginalSuperAdminId { get; }

    /// <summary>
    /// Gets the support ticket identifier associated with the active impersonation session, if active.
    /// </summary>
    string? SupportTicketId { get; }

    /// <summary>
    /// Gets the unique session identifier for tracking the active impersonation session, if active.
    /// </summary>
    Guid? SessionId { get; }

    /// <summary>
    /// Gets the target tenant identifier being impersonated, if active.
    /// </summary>
    Guid? TargetTenantId { get; }
}
