using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Integrations.Events;

namespace Master.Domain.Integrations;

/// <summary>
/// Aggregate root responsible for monitoring and tracking API rate limits and quotas for ad platforms.
/// Dispatches preventive alerts when usage reaches or exceeds configured thresholds (default: 80%).
/// </summary>
public sealed class ApiQuotaTracker : AggregateRoot<Guid>
{
    private const double DefaultWarningThreshold = 80.0;
    private const double DefaultCriticalThreshold = 95.0;

    private ApiQuotaTracker(
        Guid id,
        AdPlatform platform,
        long maxLimit,
        TimeSpan windowDuration,
        double warningThresholdPercentage,
        double criticalThresholdPercentage,
        DateTime windowStartUtc)
        : base(id)
    {
        Platform = platform;
        MaxLimit = maxLimit;
        WindowDuration = windowDuration;
        WarningThresholdPercentage = warningThresholdPercentage;
        CriticalThresholdPercentage = criticalThresholdPercentage;
        WindowStartUtc = windowStartUtc;
        CurrentConsumption = 0;
        AlertLevel = QuotaAlertLevel.Normal;
        LastUpdatedUtc = windowStartUtc;
    }

    private ApiQuotaTracker()
        : base(Guid.NewGuid())
    {
        Platform = AdPlatform.Meta;
        MaxLimit = 1;
        WindowDuration = TimeSpan.FromHours(1);
        WarningThresholdPercentage = DefaultWarningThreshold;
        CriticalThresholdPercentage = DefaultCriticalThreshold;
        WindowStartUtc = DateTime.UtcNow;
        CurrentConsumption = 0;
        AlertLevel = QuotaAlertLevel.Normal;
        LastUpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the ad network platform being tracked.
    /// </summary>
    public AdPlatform Platform { get; private set; }

    /// <summary>
    /// Gets the maximum allowed operations/calls in the active time window.
    /// </summary>
    public long MaxLimit { get; private set; }

    /// <summary>
    /// Gets the accumulated consumption within the active window.
    /// </summary>
    public long CurrentConsumption { get; private set; }

    /// <summary>
    /// Gets the percentage threshold that triggers an early warning (default: 80%).
    /// </summary>
    public double WarningThresholdPercentage { get; private set; }

    /// <summary>
    /// Gets the percentage threshold that triggers a critical alert (default: 95%).
    /// </summary>
    public double CriticalThresholdPercentage { get; private set; }

    /// <summary>
    /// Gets the duration of each rolling or fixed quota window.
    /// </summary>
    public TimeSpan WindowDuration { get; private set; }

    /// <summary>
    /// Gets the start timestamp of the current quota window in UTC.
    /// </summary>
    public DateTime WindowStartUtc { get; private set; }

    /// <summary>
    /// Gets the current alert operational state.
    /// </summary>
    public QuotaAlertLevel AlertLevel { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last recorded operation in UTC.
    /// </summary>
    public DateTime LastUpdatedUtc { get; private set; }

    /// <summary>
    /// Gets the calculated usage percentage for the active window.
    /// </summary>
    public double UsagePercentage => MaxLimit > 0
        ? Math.Round(((double)CurrentConsumption / MaxLimit) * 100.0, 2)
        : 0.0;

    /// <summary>
    /// Creates a new <see cref="ApiQuotaTracker"/> with invariant validation.
    /// </summary>
    /// <param name="platform">Target ad platform.</param>
    /// <param name="maxLimit">Maximum allowed operations in window.</param>
    /// <param name="windowDuration">Time span of the quota window.</param>
    /// <param name="warningThresholdPercentage">Percentage triggering early warning (defaults to 80%).</param>
    /// <param name="criticalThresholdPercentage">Percentage triggering critical alert (defaults to 95%).</param>
    /// <param name="windowStartUtc">Optional window start timestamp.</param>
    /// <returns>Result containing the tracker or business validation failure.</returns>
    public static Result<ApiQuotaTracker> Create(
        AdPlatform platform,
        long maxLimit,
        TimeSpan windowDuration,
        double warningThresholdPercentage = DefaultWarningThreshold,
        double criticalThresholdPercentage = DefaultCriticalThreshold,
        DateTime? windowStartUtc = null)
    {
        if (maxLimit <= 0)
        {
            return Result<ApiQuotaTracker>.Failure(
                Error.Validation("ApiQuota.InvalidMaxLimit", "O teto máximo de cota deve ser maior que zero."));
        }

        if (warningThresholdPercentage <= 0.0 || warningThresholdPercentage > 100.0)
        {
            return Result<ApiQuotaTracker>.Failure(
                Error.Validation("ApiQuota.InvalidThreshold", "O limiar de alerta preventivo deve estar entre 0% e 100%."));
        }

        if (criticalThresholdPercentage <= warningThresholdPercentage || criticalThresholdPercentage > 100.0)
        {
            criticalThresholdPercentage = Math.Max(warningThresholdPercentage, DefaultCriticalThreshold);
        }

        if (windowDuration <= TimeSpan.Zero)
        {
            return Result<ApiQuotaTracker>.Failure(
                Error.Validation("ApiQuota.InvalidWindowDuration", "A duração da janela de cota deve ser positiva."));
        }

        var start = windowStartUtc ?? DateTime.UtcNow;
        var tracker = new ApiQuotaTracker(
            Guid.NewGuid(),
            platform,
            maxLimit,
            windowDuration,
            warningThresholdPercentage,
            criticalThresholdPercentage,
            start);

        return Result<ApiQuotaTracker>.Success(tracker);
    }

    /// <summary>
    /// Records consumption of quota units, recalculating alert levels and raising warning events when appropriate.
    /// </summary>
    /// <param name="units">Number of operations or API requests consumed.</param>
    /// <param name="nowUtc">UTC timestamp of the consumption.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result RecordUsage(long units, DateTime nowUtc)
    {
        if (units <= 0)
        {
            return Result.Failure(Error.Validation("ApiQuota.InvalidUnits", "O consumo registrado deve ser maior que zero."));
        }

        var previousLevel = AlertLevel;
        CurrentConsumption += units;
        LastUpdatedUtc = nowUtc;

        var usage = UsagePercentage;

        if (usage >= 100.0)
        {
            AlertLevel = QuotaAlertLevel.Exceeded;
        }
        else if (usage >= CriticalThresholdPercentage)
        {
            AlertLevel = QuotaAlertLevel.Critical;
        }
        else if (usage >= WarningThresholdPercentage)
        {
            AlertLevel = QuotaAlertLevel.Warning;
        }
        else
        {
            AlertLevel = QuotaAlertLevel.Normal;
        }

        if (AlertLevel != previousLevel && AlertLevel >= QuotaAlertLevel.Warning)
        {
            RaiseDomainEvent(new ApiQuotaThresholdWarningEvent(
                Platform,
                AlertLevel,
                CurrentConsumption,
                MaxLimit,
                usage,
                nowUtc));
        }

        return Result.Success();
    }

    /// <summary>
    /// Resets the consumption counters for a new time window.
    /// </summary>
    /// <param name="newWindowStartUtc">New window start timestamp.</param>
    public void ResetWindow(DateTime newWindowStartUtc)
    {
        CurrentConsumption = 0;
        AlertLevel = QuotaAlertLevel.Normal;
        WindowStartUtc = newWindowStartUtc;
        LastUpdatedUtc = newWindowStartUtc;
        ClearDomainEvents();
    }

    /// <summary>
    /// Updates operational thresholds and limits.
    /// </summary>
    /// <param name="newMaxLimit">New maximum limit.</param>
    /// <param name="newWarningThreshold">New warning threshold percentage.</param>
    public Result UpdateLimits(long newMaxLimit, double newWarningThreshold)
    {
        if (newMaxLimit <= 0)
        {
            return Result.Failure(Error.Validation("ApiQuota.InvalidMaxLimit", "O teto máximo de cota deve ser maior que zero."));
        }

        if (newWarningThreshold <= 0.0 || newWarningThreshold > 100.0)
        {
            return Result.Failure(Error.Validation("ApiQuota.InvalidThreshold", "O limiar de alerta preventivo deve estar entre 0% e 100%."));
        }

        MaxLimit = newMaxLimit;
        WarningThresholdPercentage = newWarningThreshold;
        return Result.Success();
    }
}
