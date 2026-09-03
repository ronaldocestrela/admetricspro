using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Security;
using Master.Application.Repositories;
using Master.Application.Services;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Master.Infrastructure.Services;
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
    private static readonly string DefaultEncryptionKey = Convert.ToBase64String(new byte[32]
    {
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32
    });

    /// <summary>
    /// Registers the master catalog database and its related persistence services with connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The master catalog SQL Server connection string.</param>
    /// <param name="encryptionKey">Optional AES-256 base64 encryption key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMasterCatalog(
        this IServiceCollection services,
        string connectionString,
        string? encryptionKey = null)
    {
        services.AddDbContext<MasterDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(MasterDbContext).Assembly.FullName);
            });
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddSingleton<IEncryptionService>(_ => new AesEncryptionService(encryptionKey ?? DefaultEncryptionKey));
        services.AddScoped<IMasterDatabaseMigrationRunner, MasterDatabaseMigrationRunner>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantReadOnlyRepository, TenantReadOnlyRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IPlanReadOnlyRepository, PlanReadOnlyRepository>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IImpersonationSessionRepository, ImpersonationSessionRepository>();
        services.AddScoped<IImpersonationTokenService, JwtImpersonationTokenService>();
        services.AddScoped<Master.Application.Auditing.IMasterAuditRepository, MasterAuditRepository>();
        services.AddScoped<Master.Application.Auditing.IMasterAuditService, Master.Application.Auditing.MasterAuditService>();
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Master.Application.Auditing.AuditImpersonationBehavior<,>));
        services.AddScoped<Master.Application.Integrations.Repositories.IApiQuotaRepository, ApiQuotaRepository>();
        services.AddScoped<Master.Application.Integrations.Repositories.ITenantApiConnectionRepository, TenantApiConnectionRepository>();
        services.AddScoped<Master.Application.Integrations.Services.IApiQuotaTrackerService, Master.Infrastructure.Integrations.InMemoryApiQuotaTracker>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>
    /// Registers the background dunning service and its configuration options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration delegate for dunning options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDunningBackgroundService(
        this IServiceCollection services,
        Action<DunningOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<DunningOptions>();
        }

        services.AddHostedService<DunningBackgroundService>();
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
