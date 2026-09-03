using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Application.Services;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Master.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering and applying master database migrations and catalog persistence.
/// </summary>
public static class MasterDatabaseMigrationExtensions
{
    /// <summary>
    /// Registers the master catalog database and its related persistence services with connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The master catalog SQL Server connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMasterCatalog(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<MasterDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(MasterDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IMasterDatabaseMigrationRunner, MasterDatabaseMigrationRunner>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>
    /// Registers the master database migration runner in the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMasterDatabaseMigration(this IServiceCollection services)
    {
        services.AddScoped<IMasterDatabaseMigrationRunner, MasterDatabaseMigrationRunner>();
        return services;
    }

    /// <summary>
    /// Applies pending master database migrations on the host application during startup.
    /// </summary>
    /// <param name="host">The application host.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or migration failure.</returns>
    public static async Task<Result> ApplyMasterDatabaseMigrationsAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMasterDatabaseMigrationRunner>();
        return await runner.ApplyMigrationsAsync(cancellationToken);
    }
}

/// <summary>
/// Hosted service that automatically applies master database migrations on application startup.
/// </summary>
public sealed class MasterDatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MasterDatabaseMigrationHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MasterDatabaseMigrationHostedService"/> class.
    /// </summary>
    /// <param name="serviceProvider">Root service provider.</param>
    /// <param name="logger">Logger instance.</param>
    public MasterDatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<MasterDatabaseMigrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting automated master database migrations via hosted service...");
        using var scope = _serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMasterDatabaseMigrationRunner>();
        var result = await runner.ApplyMigrationsAsync(cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Automated master database migration failed: {ErrorCode} - {ErrorMessage}",
                result.Error.Code, result.Error.Description);
            throw new InvalidOperationException($"Master database migration failed: {result.Error.Description}");
        }

        _logger.LogInformation("Automated master database migrations completed successfully.");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
