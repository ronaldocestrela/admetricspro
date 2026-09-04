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
        const int maxRetries = 5;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Applying master database migrations (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                await _masterDbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Master database migrations applied successfully.");

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Master database migration was cancelled.");
                return Result.Failure(Error.Failure("MasterMigration.ExecutionFailed", "Master database migration was cancelled."));
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientStartupException(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Transient connection failure during master database migration (pre-login handshake or server warming up). Retrying in {DelaySeconds}s (attempt {Attempt}/{MaxRetries})...",
                    retryDelay.TotalSeconds,
                    attempt,
                    maxRetries);

                try
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return Result.Failure(Error.Failure("MasterMigration.ExecutionFailed", "Master database migration was cancelled during retry wait."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply master database migrations.");
                return Result.Failure(Error.Failure("MasterMigration.ExecutionFailed", $"Failed to apply master database migrations: {ex.Message}"));
            }
        }

        return Result.Failure(Error.Failure("MasterMigration.ExecutionFailed", "Failed to apply master database migrations after multiple retry attempts."));
    }

    private static bool IsTransientStartupException(Exception ex)
    {
        // Intercepta falhas de handshake pré-login, recusa temporária de conexão ou timeout de inicialização do SQL Server
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("pre-login handshake") ||
               message.Contains("handshake") ||
               message.Contains("tcp provider") ||
               message.Contains("network-related") ||
               message.Contains("server was not found") ||
               message.Contains("login failed");
    }
}
