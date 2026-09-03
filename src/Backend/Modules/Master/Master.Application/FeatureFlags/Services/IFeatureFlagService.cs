using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.DTOs;
using Master.Domain.Integrations;

namespace Master.Application.FeatureFlags.Services;

/// <summary>
/// High-performance service providing feature flag evaluations and operational kill switches with in-memory caching.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Fast in-memory check to determine if a feature flag is enabled for the optional tenant context.
    /// </summary>
    /// <param name="flagKey">Unique flag key.</param>
    /// <param name="tenantId">Optional tenant ID context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if enabled/active; otherwise, false.</returns>
    Task<bool> IsFeatureEnabledAsync(string flagKey, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates if cross-network automation engine or a specific ad network engine is frozen by an operational Kill Switch.
    /// Checks both the global automation kill switch and any network-specific kill switch.
    /// </summary>
    /// <param name="platform">Optional ad platform to check specifically.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if automation is halted/frozen; otherwise, false.</returns>
    Task<bool> IsAutomationFrozenAsync(AdPlatform? platform = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a feature flag and returns a Result containing the evaluation outcome.
    /// </summary>
    /// <param name="flagKey">Flag key.</param>
    /// <param name="tenantId">Optional tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing true/false or NotFound error.</returns>
    Task<Result<bool>> EvaluateAsync(string flagKey, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Engages/activates an emergency Kill Switch, halting the protected system and recording an immutable audit log.
    /// </summary>
    /// <param name="flagKey">Kill switch flag key.</param>
    /// <param name="reason">Mandatory operational justification.</param>
    /// <param name="triggeredBy">Operator or process name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ActivateKillSwitchAsync(string flagKey, string reason, string triggeredBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disengages/deactivates an emergency Kill Switch, resuming operations and recording an immutable audit log.
    /// </summary>
    /// <param name="flagKey">Kill switch flag key.</param>
    /// <param name="reason">Mandatory operational justification.</param>
    /// <param name="triggeredBy">Operator or process name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeactivateKillSwitchAsync(string flagKey, string reason, string triggeredBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all feature flags and kill switches formatted as DTOs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of feature flag DTOs.</returns>
    Task<Result<IReadOnlyList<FeatureFlagDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single feature flag by key.
    /// </summary>
    /// <param name="flagKey">Unique flag key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Feature flag DTO or NotFound error.</returns>
    Task<Result<FeatureFlagDto>> GetByKeyAsync(string flagKey, CancellationToken cancellationToken = default);
}
