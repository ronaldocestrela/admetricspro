using BuildingBlocks.Application.Security;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Extension methods for registering security, impersonation context, and data masking services.
/// </summary>
public static class SecurityServiceExtensions
{
    /// <summary>
    /// Registers impersonation context accessor and billing data masking services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IImpersonationContextAccessor, ImpersonationContextAccessor>();
        services.AddScoped<IBillingDataMasker, BillingDataMasker>();
        return services;
    }
}
