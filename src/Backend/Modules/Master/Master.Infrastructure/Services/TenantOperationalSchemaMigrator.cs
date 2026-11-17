using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Abstractions;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Master.Infrastructure.Services;

/// <summary>
/// Aplica migrações EF do schema operacional do inquilino via <see cref="TenantOperationalDbContext"/>.
/// </summary>
public sealed class TenantOperationalSchemaMigrator : ITenantOperationalSchemaMigrator
{
    private readonly ITenantConnectionResolver _connectionResolver;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantOperationalSchemaMigrator"/>.
    /// </summary>
    /// <param name="connectionResolver">Resolvedor de connection string do tenant.</param>
    public TenantOperationalSchemaMigrator(ITenantConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver ?? throw new ArgumentNullException(nameof(connectionResolver));
    }

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Result.Failure(
                Error.Validation("TenantOperational.ConnectionStringRequired", "A connection string do tenant é obrigatória."));
        }

        try
        {
            var options = new DbContextOptionsBuilder<TenantOperationalDbContext>()
                .UseSqlServer(connectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;

            await using var context = new TenantOperationalDbContext(options);
            await context.Database.MigrateAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(Error.Failure("TenantOperational.MigrationFailed", "Migração operacional cancelada."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("TenantOperational.MigrationFailed", $"Falha ao migrar schema operacional: {ex.Message}"));
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