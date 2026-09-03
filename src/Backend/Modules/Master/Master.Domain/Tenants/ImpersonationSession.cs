using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Tenants;

/// <summary>
/// Domain aggregate root representing a temporary, audited tenant impersonation session (Shadow Mode).
/// </summary>
public sealed class ImpersonationSession : AggregateRoot<Guid>
{
    private ImpersonationSession(
        Guid id,
        TenantId tenantId,
        Guid superAdminId,
        string supportTicketId,
        string reason,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        SuperAdminId = superAdminId;
        SupportTicketId = supportTicketId;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = null;
        RevokeReason = null;
    }

    private ImpersonationSession()
        : base(Guid.NewGuid())
    {
        TenantId = new TenantId(Guid.Empty);
        SuperAdminId = Guid.Empty;
        SupportTicketId = string.Empty;
        Reason = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the tenant identifier being impersonated.
    /// </summary>
    public TenantId TenantId { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the SuperAdmin or support engineer.
    /// </summary>
    public Guid SuperAdminId { get; private set; }

    /// <summary>
    /// Gets the support ticket identifier justifying this session.
    /// </summary>
    public string SupportTicketId { get; private set; }

    /// <summary>
    /// Gets the documented reason for requesting impersonation.
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// Gets the UTC creation timestamp of the session.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC expiration timestamp of the session.
    /// </summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC revocation timestamp if the session was explicitly terminated early.
    /// </summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Gets the documented reason why the session was revoked.
    /// </summary>
    public string? RevokeReason { get; private set; }

    /// <summary>
    /// Factory method to create an audited impersonation session.
    /// </summary>
    /// <param name="tenantId">Target tenant identifier.</param>
    /// <param name="superAdminId">SuperAdmin initiating the session.</param>
    /// <param name="supportTicketId">Mandatory support ticket identifier.</param>
    /// <param name="reason">Audited reason with at least 10 characters.</param>
    /// <param name="durationMinutes">Session lifetime between 5 and 120 minutes.</param>
    /// <param name="utcNow">Current UTC timestamp.</param>
    /// <returns>Result containing the session entity or validation errors.</returns>
    public static Result<ImpersonationSession> Create(
        TenantId tenantId,
        Guid superAdminId,
        string supportTicketId,
        string reason,
        int durationMinutes,
        DateTime utcNow)
    {
        if (tenantId == null || tenantId.Value == Guid.Empty)
        {
            return Result<ImpersonationSession>.Failure(
                Error.Validation("Impersonation.InvalidTenant", "Target TenantId cannot be empty."));
        }

        if (superAdminId == Guid.Empty)
        {
            return Result<ImpersonationSession>.Failure(
                Error.Validation("Impersonation.InvalidSuperAdmin", "SuperAdminId cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(supportTicketId))
        {
            return Result<ImpersonationSession>.Failure(ImpersonationErrors.InvalidTicket);
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            return Result<ImpersonationSession>.Failure(ImpersonationErrors.InvalidReason);
        }

        if (durationMinutes < 5 || durationMinutes > 120)
        {
            return Result<ImpersonationSession>.Failure(
                Error.Validation("Impersonation.InvalidDuration", "Impersonation duration must be between 5 and 120 minutes."));
        }

        var expiresAtUtc = utcNow.AddMinutes(durationMinutes);
        var session = new ImpersonationSession(
            Guid.NewGuid(),
            tenantId,
            superAdminId,
            supportTicketId.Trim(),
            reason.Trim(),
            utcNow,
            expiresAtUtc);

        return Result<ImpersonationSession>.Success(session);
    }

    /// <summary>
    /// Determines if the session is currently active at the given reference time.
    /// </summary>
    /// <param name="referenceUtc">UTC reference timestamp to evaluate.</param>
    /// <returns>True if the session has not been revoked and has not expired.</returns>
    public bool IsActiveAt(DateTime referenceUtc)
    {
        return RevokedAtUtc is null && referenceUtc < ExpiresAtUtc;
    }

    /// <summary>
    /// Revokes the session immediately, terminating active impersonation capabilities.
    /// </summary>
    /// <param name="reason">Reason for revocation.</param>
    /// <param name="revokedAtUtc">UTC timestamp of revocation.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result Revoke(string reason, DateTime revokedAtUtc)
    {
        if (RevokedAtUtc is not null)
        {
            return Result.Failure(ImpersonationErrors.SessionRevoked);
        }

        if (revokedAtUtc >= ExpiresAtUtc)
        {
            return Result.Failure(ImpersonationErrors.SessionExpired);
        }

        RevokedAtUtc = revokedAtUtc;
        RevokeReason = string.IsNullOrWhiteSpace(reason) ? "Manual session termination" : reason.Trim();

        return Result.Success();
    }
}
