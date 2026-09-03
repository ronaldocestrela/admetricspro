namespace Master.Application.Billing.Dunning;

/// <summary>
/// Encapsulates the execution summary metrics of an automated or manual dunning processing cycle.
/// </summary>
/// <param name="EvaluatedCount">Total number of tenants assessed during the cycle.</param>
/// <param name="TransitionsCount">Number of tenants that transitioned between dunning stages.</param>
/// <param name="SuspendedCount">Number of tenants newly suspended due to reaching the maximum overdue threshold (D+14).</param>
/// <param name="UnchangedCount">Number of tenants evaluated whose dunning stage did not change.</param>
/// <param name="ExecutedAtUtc">Timestamp when the cycle executed.</param>
public sealed record DunningExecutionSummary(
    int EvaluatedCount,
    int TransitionsCount,
    int SuspendedCount,
    int UnchangedCount,
    DateTime ExecutedAtUtc);
