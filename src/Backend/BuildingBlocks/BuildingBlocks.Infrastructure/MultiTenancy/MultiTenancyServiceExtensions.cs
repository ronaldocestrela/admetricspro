using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy.Strategies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Service collection and application builder extensions for configuring multi-tenant resolution services.
/// </summary>
public static class MultiTenancyServiceExtensions
{
    /// <summary>
    /// Registers multi-tenancy core abstractions, strategies, and scoped context accessors.
    /// </summary>
    /// <param name="services">Target service collection.</param>
    /// <param name="configure">Optional delegate to configure <see cref="TenantResolutionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        Action<TenantResolutionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<TenantResolutionOptions>(_ => { });
        }

        services.TryAddScoped<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<ITenantContextAccessor>().TenantContext);

        services.AddTransient<ITenantIdentificationStrategy, HeaderTenantIdentificationStrategy>();
        services.AddTransient<ITenantIdentificationStrategy, JwtClaimTenantIdentificationStrategy>();
        services.AddTransient<ITenantIdentificationStrategy, SubdomainTenantIdentificationStrategy>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="TenantIdentificationMiddleware"/> to the ASP.NET Core request pipeline.
    /// </summary>
    /// <param name="app">Target application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<TenantIdentificationMiddleware>();
    }
}
