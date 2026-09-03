using Master.Domain.Integrations;

namespace Master.Application.Integrations.Repositories;

/// <summary>
/// Repository interface for persisting and querying ad platform API rate limits and quota tracking aggregates.
/// </summary>
public interface IApiQuotaRepository
{
    /// <summary>
    /// Finds the quota tracker for a specific ad platform.
    /// </summary>
    /// <param name="platform">Target platform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching quota tracker aggregate, or null if not found.</returns>
    Task<ApiQuotaTracker?> GetByPlatformAsync(AdPlatform platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all configured platform quota trackers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only list of quota trackers.</returns>
    Task<IReadOnlyList<ApiQuotaTracker>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new quota tracker aggregate.
    /// </summary>
    /// <param name="tracker">Tracker aggregate to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(ApiQuotaTracker tracker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing quota tracker aggregate.
    /// </summary>
    /// <param name="tracker">Tracker aggregate to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(ApiQuotaTracker tracker, CancellationToken cancellationToken = default);
}
