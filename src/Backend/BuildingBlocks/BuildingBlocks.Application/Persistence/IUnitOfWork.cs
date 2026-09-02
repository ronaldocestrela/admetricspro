namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// Coordinates write operations into a single commit boundary.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of affected records.</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}