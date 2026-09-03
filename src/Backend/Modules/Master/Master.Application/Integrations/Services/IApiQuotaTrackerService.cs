using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Domain.Integrations;

namespace Master.Application.Integrations.Services;

/// <summary>
/// Service coordinating real-time API quota tracking, in-memory counter aggregation,
/// threshold warning alerts, and state persistence.
/// </summary>
public interface IApiQuotaTrackerService
{
    /// <summary>
    /// Records usage units consumed by an ad platform operation.
    /// </summary>
    /// <param name="platform">Target ad platform.</param>
    /// <param name="units">Number of operations/calls consumed.</param>
    /// <param name="nowUtc">Timestamp of the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the updated quota status or error.</returns>
    Task<Result<PlatformQuotaStatusDto>> RecordUsageAsync(
        AdPlatform platform,
        long units,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current quota status for all supported ad platforms.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of platform quota statuses.</returns>
    Task<IReadOnlyList<PlatformQuotaStatusDto>> GetAllQuotaStatusesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current quota status for a specific ad platform.
    /// </summary>
    /// <param name="platform">Target platform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Platform quota status or null if not configured.</returns>
    Task<PlatformQuotaStatusDto?> GetPlatformQuotaStatusAsync(
        AdPlatform platform,
        CancellationToken cancellationToken = default);
}
