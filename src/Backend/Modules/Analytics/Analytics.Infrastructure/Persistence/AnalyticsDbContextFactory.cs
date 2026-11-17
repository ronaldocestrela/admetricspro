using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;

namespace Analytics.Infrastructure.Persistence;

/// <summary>
/// Fábrica de <see cref="AnalyticsDbContext"/> com connection string resolvida por tenant.
/// </summary>
public sealed class AnalyticsDbContextFactory : ITenantDbContextFactory<AnalyticsDbContext>
{
    private readonly ITenantConnectionResolver _connectionResolver;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AnalyticsDbContextFactory"/>.
    /// </summary>
    /// <param name="connectionResolver">Resolvedor de connection string.</param>
    public AnalyticsDbContextFactory(ITenantConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver ?? throw new ArgumentNullException(nameof(connectionResolver));
    }

    /// <inheritdoc />
    public async Task<Result<AnalyticsDbContext>> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await _connectionResolver.ResolveCurrentTenantConnectionStringAsync(cancellationToken);
        if (connectionStringResult.IsFailure)
        {
            return Result<AnalyticsDbContext>.Failure(connectionStringResult.Error);
        }

        return CreateContext(connectionStringResult.Value);
    }

    /// <inheritdoc />
    public async Task<Result<AnalyticsDbContext>> CreateDbContextAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await _connectionResolver.ResolveConnectionStringAsync(tenantId, cancellationToken);
        if (connectionStringResult.IsFailure)
        {
            return Result<AnalyticsDbContext>.Failure(connectionStringResult.Error);
        }

        return CreateContext(connectionStringResult.Value);
    }

    private static Result<AnalyticsDbContext> CreateContext(string connectionString)
    {
        try
        {
            var options = AnalyticsDbContext.CreateOptions(connectionString);
            return Result<AnalyticsDbContext>.Success(new AnalyticsDbContext(options));
        }
        catch (Exception ex)
        {
            return Result<AnalyticsDbContext>.Failure(
                Error.Failure("Analytics.DbContextCreationFailed", $"Falha ao criar AnalyticsDbContext: {ex.Message}"));
        }
    }
}
