using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.DependencyInjection;

/// <summary>
/// Provides extension methods for registering in-memory messaging and validation pipeline behaviors.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR, FluentValidation validators, and pipeline behaviors from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers, validators, and notifications.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        var targetAssemblies = assemblies is { Length: > 0 }
            ? assemblies.Distinct().ToArray()
            : [Assembly.GetCallingAssembly()];

        services.AddLogging();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(targetAssemblies);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(targetAssemblies, includeInternalTypes: true);

        return services;
    }
}
