using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tenants.Application.DependencyInjection;

/// <summary>
/// Métodos de extensão de injeção de dependência para registro dos serviços e manipuladores do módulo Tenants.Application.
/// </summary>
public static class TenantsApplicationServiceExtensions
{
    /// <summary>
    /// Registra manipuladores MediatR, validadores FluentValidation e comportamentos do módulo Tenants.
    /// </summary>
    /// <param name="services">A coleção de serviços da aplicação.</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddTenantsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessaging(typeof(TenantsApplicationServiceExtensions).Assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Tenants.Application.Rbac.Behaviors.TenantAuthorizationBehavior<,>));

        return services;
    }
}
