namespace Master.Domain.Integrations;

/// <summary>
/// Status representing the health of a tenant's ad platform integration credentials.
/// </summary>
public enum ApiConnectionStatus
{
    /// <summary>
    /// Integration is connected with valid and active OAuth tokens.
    /// </summary>
    Connected = 0,

    /// <summary>
    /// Token is valid but expiring soon (within 7 days).
    /// </summary>
    ExpiringSoon = 1,

    /// <summary>
    /// Token has expired and requires tenant reconnection.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Access was revoked by the user on the advertising platform.
    /// </summary>
    Revoked = 3,

    /// <summary>
    /// Connection failed or credentials could not be verified.
    /// </summary>
    Disconnected = 4
}
