namespace Master.Application.Tenants.Commands.ImpersonateTenant;

/// <summary>
/// Represents the result and contextual access token for an active tenant impersonation session.
/// </summary>
/// <param name="AccessToken">Contextual JSON Web Token containing impersonation claims.</param>
/// <param name="TokenType">Token scheme type, defaults to 'Bearer'.</param>
/// <param name="ExpiresInSeconds">Duration in seconds until token expiration.</param>
/// <param name="SessionId">Unique identifier of the persisted impersonation session.</param>
/// <param name="TenantId">Target tenant identifier.</param>
/// <param name="TenantName">Company name of the target tenant.</param>
/// <param name="SuperAdminId">SuperAdmin identifier who initiated the session.</param>
/// <param name="SupportTicketId">Associated support ticket reference.</param>
/// <param name="ExpiresAtUtc">UTC timestamp when this session expires.</param>
public sealed record ImpersonateTenantResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    Guid SessionId,
    Guid TenantId,
    string TenantName,
    Guid SuperAdminId,
    string SupportTicketId,
    DateTime ExpiresAtUtc);
