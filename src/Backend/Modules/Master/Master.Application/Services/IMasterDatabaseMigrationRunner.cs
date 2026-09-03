using BuildingBlocks.Domain.Primitives;

namespace Master.Application.Services;

/// <summary>
/// Defines the automated migration pipeline runner for the master catalog database.
/// </summary>
public interface IMasterDatabaseMigrationRunner
{
    /// <summary>
    /// Applies pending Entity Framework Core migrations to the master catalog database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or a failure error.</returns>
    Task<Result> ApplyMigrationsAsync(CancellationToken cancellationToken = default);
}
