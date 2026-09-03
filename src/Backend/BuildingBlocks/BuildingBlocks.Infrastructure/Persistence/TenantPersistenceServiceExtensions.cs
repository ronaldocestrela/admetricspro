using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Service collection extensions for registering multi-tenant persistence infrastructure.
/// </summary>
public static class TenantPersistenceServiceExtensions
{
    /// <summary>
    /// Registers tenant persistence infrastructure including dynamic DbContext factory and connection holder.
    /// </summary>
    /// <typeparam name="TContext">The tenant DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTenantPersistence<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<ITenantConnectionHolder, TenantConnectionHolder>();
        services.AddScoped<ITenantDbContextFactory<TContext>, TenantDbContextFactory<TContext>>();
        return services;
    }
}
