using BuildingBlocks.Domain.Primitives;
using Master.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Master.Infrastructure.Persistence;

/// <summary>
/// Executes pending Entity Framework Core migrations on the master catalog database.
/// </summary>
public sealed class MasterDatabaseMigrationRunner : IMasterDatabaseMigrationRunner
{
    private readonly MasterDbContext _masterDbContext;
    private readonly ILogger<MasterDatabaseMigrationRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MasterDatabaseMigrationRunner"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog database context.</param>
    /// <param name="logger">Logger instance.</param>
    public MasterDatabaseMigrationRunner(
        MasterDbContext masterDbContext,
        ILogger<MasterDatabaseMigrationRunner> logger)
    {
        _masterDbContext = masterDbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Applying master database migrations...");
            await _masterDbContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Master database migrations applied successfully.");

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Master database migration was cancelled.");
            return Result.Failure(Error.Failure("MasterMigration.ExecutionFailed", "Master database migration was cancelled."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply master database migrations.");
            return Result.Failure(Error.Failure("MasterMigration.ExecutionFailed", $"Failed to apply master database migrations: {ex.Message}"));
        }
    }
}
