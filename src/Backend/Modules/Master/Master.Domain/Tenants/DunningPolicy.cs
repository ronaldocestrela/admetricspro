namespace Master.Domain.Tenants;

/// <summary>
/// Domain policy defining the rules, progressive thresholds, and functional restrictions of the dunning engine.
/// </summary>
public static class DunningPolicy
{
    /// <summary>
    /// Days threshold for Stage 1: deactivation of cross-network automations (D+3).
    /// </summary>
    public const int AutomationsDisabledDays = 3;

    /// <summary>
    /// Days threshold for Stage 2: blocking analytical reports and attribution dashboards (D+7).
    /// </summary>
    public const int ReportsBlockedDays = 7;

    /// <summary>
    /// Days threshold for Stage 3: total suspension and login blockage (D+14).
    /// </summary>
    public const int TotalSuspensionDays = 14;

    /// <summary>
    /// Calculates the number of full days overdue from the due date to the reference UTC timestamp.
    /// </summary>
    /// <param name="dueDateUtc">The payment due date.</param>
    /// <param name="referenceUtc">The reference UTC timestamp (usually DateTime.UtcNow).</param>
    /// <returns>Non-negative number of days overdue, or 0 if not yet overdue.</returns>
    public static int CalculateDaysOverdue(DateTime dueDateUtc, DateTime referenceUtc)
    {
        if (referenceUtc <= dueDateUtc)
        {
            return 0;
        }

        var difference = referenceUtc.Date - dueDateUtc.Date;
        return Math.Max(0, difference.Days);
    }

    /// <summary>
    /// Evaluates the appropriate dunning stage based on the payment due date and current UTC reference time.
    /// </summary>
    /// <param name="dueDateUtc">The payment due timestamp in UTC, or null if in good standing.</param>
    /// <param name="referenceUtc">The current UTC timestamp.</param>
    /// <returns>The calculated <see cref="DunningStage"/>.</returns>
    public static DunningStage EvaluateStage(DateTime? dueDateUtc, DateTime referenceUtc)
    {
        if (!dueDateUtc.HasValue)
        {
            return DunningStage.None;
        }

        var daysOverdue = CalculateDaysOverdue(dueDateUtc.Value, referenceUtc);

        if (daysOverdue >= TotalSuspensionDays)
        {
            return DunningStage.LoginBlocked;
        }

        if (daysOverdue >= ReportsBlockedDays)
        {
            return DunningStage.ReportsBlocked;
        }

        if (daysOverdue >= AutomationsDisabledDays)
        {
            return DunningStage.AutomationsDisabled;
        }

        return DunningStage.None;
    }

    /// <summary>
    /// Determines whether campaign automations and background rule executors are allowed at the given stage.
    /// </summary>
    /// <param name="stage">The current dunning stage.</param>
    /// <returns>True if automations may run; otherwise false.</returns>
    public static bool AreAutomationsAllowed(DunningStage stage)
    {
        return stage == DunningStage.None;
    }

    /// <summary>
    /// Determines whether analytical reports, dashboards, and export features are allowed at the given stage.
    /// </summary>
    /// <param name="stage">The current dunning stage.</param>
    /// <returns>True if reports may be viewed; otherwise false.</returns>
    public static bool AreReportsAllowed(DunningStage stage)
    {
        return stage < DunningStage.ReportsBlocked;
    }

    /// <summary>
    /// Determines whether user authentication and interactive platform access are allowed at the given stage.
    /// </summary>
    /// <param name="stage">The current dunning stage.</param>
    /// <returns>True if login is permitted; otherwise false.</returns>
    public static bool IsLoginAllowed(DunningStage stage)
    {
        return stage < DunningStage.LoginBlocked;
    }
}
