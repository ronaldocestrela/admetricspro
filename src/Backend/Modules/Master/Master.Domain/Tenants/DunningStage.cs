namespace Master.Domain.Tenants;

/// <summary>
/// Represents the progressive stages of payment default (dunning) for a tenant.
/// </summary>
public enum DunningStage
{
    /// <summary>
    /// Tenant is in good financial standing or within initial grace period (D+0 to D+2). No functional restrictions.
    /// </summary>
    None = 0,

    /// <summary>
    /// Stage 1 (D+3 to D+6): Campaign automations and rule triggers are deactivated to prevent uncontrolled ad spend.
    /// </summary>
    AutomationsDisabled = 1,

    /// <summary>
    /// Stage 2 (D+7 to D+13): Analytical reports and attribution queries are blocked, in addition to automations.
    /// </summary>
    ReportsBlocked = 2,

    /// <summary>
    /// Stage 3 (D+14+): Total operational suspension and login access blocked.
    /// </summary>
    LoginBlocked = 3
}
