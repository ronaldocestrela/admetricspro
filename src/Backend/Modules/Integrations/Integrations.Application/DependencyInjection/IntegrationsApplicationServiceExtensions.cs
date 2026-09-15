using Microsoft.Extensions.DependencyInjection;

namespace Integrations.Application.DependencyInjection;

/// <summary>
/// Métodos de extensão para registro de dependências do módulo Integrations.Application.
/// </summary>
public static class IntegrationsApplicationServiceExtensions
{
    /// <summary>
    /// Registra handlers CQRS e validações do módulo de Integrações.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>A coleção de serviços para encadeamento.</returns>
    public static IServiceCollection AddIntegrationsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IntegrationsApplicationServiceExtensions).Assembly);
        });

        return services;
    }
}
