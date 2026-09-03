namespace Master.Infrastructure.Services;

/// <summary>
/// Configuration options for the automated background dunning engine.
/// </summary>
public sealed class DunningOptions
{
    /// <summary>
    /// Configuration section key in appsettings.json.
    /// </summary>
    public const string SectionName = "Dunning";

    /// <summary>
    /// Gets or sets a value indicating whether the background dunning service is enabled. Defaults to true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the execution interval in minutes. Defaults to 1440 (24 hours).
    /// </summary>
    public int IntervalMinutes { get; set; } = 1440;
}
