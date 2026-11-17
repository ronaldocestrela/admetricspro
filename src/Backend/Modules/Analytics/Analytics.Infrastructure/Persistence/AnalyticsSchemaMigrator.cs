using Analytics.Application.Abstractions;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Persistence;

/// <summary>
/// Aplica migrações EF do schema analytics.
/// </summary>
public sealed class AnalyticsSchemaMigrator : IAnalyticsSchemaMigrator
{
    private readonly ITenantConnectionResolver _connectionResolver;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AnalyticsSchemaMigrator"/>.
    /// </summary>
    /// <param name="connectionResolver">Resolvedor de connection string do tenant.</param>
    public AnalyticsSchemaMigrator(ITenantConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver ?? throw new ArgumentNullException(nameof(connectionResolver));
    }

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Result.Failure(
                Error.Validation("Analytics.ConnectionStringRequired", "A connection string do tenant é obrigatória."));
        }

        try
        {
            await using var context = new AnalyticsDbContext(AnalyticsDbContext.CreateOptions(connectionString));
            await context.Database.MigrateAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(Error.Failure("Analytics.MigrationFailed", "Migração analytics cancelada."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Analytics.MigrationFailed", $"Falha ao migrar schema analytics: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ApplyForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await _connectionResolver.ResolveConnectionStringAsync(tenantId, cancellationToken);
        if (connectionStringResult.IsFailure)
        {
            return Result.Failure(connectionStringResult.Error);
        }

        return await ApplyAsync(connectionStringResult.Value, cancellationToken);
    }
}
