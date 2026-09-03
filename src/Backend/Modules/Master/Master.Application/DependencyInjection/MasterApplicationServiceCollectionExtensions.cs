using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Master.Application.DependencyInjection;

/// <summary>
/// Service collection extension methods for registering Master.Application module services and messaging handlers.
/// </summary>
public static class MasterApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and application behaviors for the Master module.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMasterApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessaging(typeof(MasterApplicationServiceCollectionExtensions).Assembly);
        services.AddScoped<Billing.Dunning.IDunningEngineService, Billing.Dunning.DunningEngineService>();

        return services;
    }
}
