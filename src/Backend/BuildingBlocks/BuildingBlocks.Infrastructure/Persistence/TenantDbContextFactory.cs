using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Factory that dynamically creates DbContext instances configured with connection strings resolved from <see cref="ITenantConnectionResolver"/>.
/// </summary>
/// <typeparam name="TContext">The tenant DbContext type.</typeparam>
public sealed class TenantDbContextFactory<TContext> : ITenantDbContextFactory<TContext> where TContext : DbContext
{
    private readonly ITenantConnectionResolver _connectionResolver;
    private readonly Func<DbContextOptions<TContext>, TContext>? _contextCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantDbContextFactory{TContext}"/> class.
    /// </summary>
    /// <param name="connectionResolver">The tenant connection resolver service.</param>
    /// <param name="contextCreator">Optional custom creator delegate for instantiating TContext.</param>
    public TenantDbContextFactory(
        ITenantConnectionResolver connectionResolver,
        Func<DbContextOptions<TContext>, TContext>? contextCreator = null)
    {
        _connectionResolver = connectionResolver;
        _contextCreator = contextCreator;
    }

    /// <inheritdoc />
    public async Task<Result<TContext>> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await _connectionResolver.ResolveCurrentTenantConnectionStringAsync(cancellationToken);
        if (connectionStringResult.IsFailure)
        {
            return Result<TContext>.Failure(connectionStringResult.Error);
        }

        return CreateContextForConnectionString(connectionStringResult.Value);
    }

    /// <inheritdoc />
    public async Task<Result<TContext>> CreateDbContextAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connectionStringResult = await _connectionResolver.ResolveConnectionStringAsync(tenantId, cancellationToken);
        if (connectionStringResult.IsFailure)
        {
            return Result<TContext>.Failure(connectionStringResult.Error);
        }

        return CreateContextForConnectionString(connectionStringResult.Value);
    }

    private Result<TContext> CreateContextForConnectionString(string connectionString)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseSqlServer(connectionString);

            var options = optionsBuilder.Options;

            TContext context = _contextCreator is not null
                ? _contextCreator(options)
                : (TContext)Activator.CreateInstance(typeof(TContext), options)!;

            return Result<TContext>.Success(context);
        }
        catch (Exception ex)
        {
            return Result<TContext>.Failure(Error.Failure("TenantDbContext.CreationFailed", $"Failed to instantiate DbContext: {ex.Message}"));
        }
    }
}
