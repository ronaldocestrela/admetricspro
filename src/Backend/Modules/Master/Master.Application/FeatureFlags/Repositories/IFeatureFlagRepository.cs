using Master.Domain.FeatureFlags;

namespace Master.Application.FeatureFlags.Repositories;

/// <summary>
/// Repository abstraction for managing FeatureFlag and KillSwitch persistence in the Master catalog.
/// </summary>
public interface IFeatureFlagRepository
{
    /// <summary>
    /// Finds a feature flag by its unique key.
    /// </summary>
    /// <param name="key">Unique key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The feature flag instance if found; otherwise, null.</returns>
    Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a feature flag by its unique identifier.
    /// </summary>
    /// <param name="id">Primary key identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The feature flag instance if found; otherwise, null.</returns>
    Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all feature flags and operational kill switches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only collection of feature flags.</returns>
    Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active emergency kill switches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only collection of operational kill switches.</returns>
    Task<IReadOnlyList<FeatureFlag>> GetKillSwitchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new feature flag to the repository.
    /// </summary>
    /// <param name="flag">Feature flag aggregate to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(FeatureFlag flag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing feature flag in the repository.
    /// </summary>
    /// <param name="flag">Feature flag aggregate to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
}
