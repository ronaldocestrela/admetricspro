using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Aplica migrações EF do schema social usando connection string explícita ou resolvida por tenant.
/// </summary>
public sealed class SocialPublishingSchemaMigrator : ISocialPublishingSchemaMigrator
{
    private readonly ITenantConnectionResolver _connectionResolver;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialPublishingSchemaMigrator"/>.
    /// </summary>
    /// <param name="connectionResolver">Resolvedor de connection string do tenant.</param>
    public SocialPublishingSchemaMigrator(ITenantConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver ?? throw new ArgumentNullException(nameof(connectionResolver));
    }

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Result.Failure(
                Error.Validation("Social.ConnectionStringRequired", "A connection string do tenant é obrigatória."));
        }

        try
        {
            await using var context = new SocialPublishingDbContext(SocialPublishingDbContext.CreateOptions(connectionString));
            await context.Database.MigrateAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(SocialErrors.MigrationFailed);
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure(SocialErrors.MigrationFailed.Code, $"{SocialErrors.MigrationFailed.Description} {ex.Message}"));
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
