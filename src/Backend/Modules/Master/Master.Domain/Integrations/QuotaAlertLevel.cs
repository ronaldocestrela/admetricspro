namespace Master.Domain.Integrations;

/// <summary>
/// Represents the operational warning state of an API quota limit.
/// </summary>
public enum QuotaAlertLevel
{
    /// <summary>
    /// Consumption is within safe limits (below 80%).
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Preventive warning: consumption reached or exceeded the 80% threshold.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Critical limit: consumption reached or exceeded 95%, imminent exhaustion.
    /// </summary>
    Critical = 2,

    /// <summary>
    /// Quota ceiling reached or exceeded (100%+), potential rate-limiting/throttling.
    /// </summary>
    Exceeded = 3
}
