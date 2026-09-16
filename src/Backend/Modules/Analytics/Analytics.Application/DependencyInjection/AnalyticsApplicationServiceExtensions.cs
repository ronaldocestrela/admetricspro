using Microsoft.Extensions.DependencyInjection;

namespace Analytics.Application.DependencyInjection;

/// <summary>
/// Métodos de extensão para registro de dependências da camada de aplicação do módulo Analytics.
/// </summary>
public static class AnalyticsApplicationServiceExtensions
{
    /// <summary>
    /// Registra os manipuladores de comandos e consultas CQRS do módulo Analytics no MediatR.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A coleção de serviços configurada para encadeamento fluente.</returns>
    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AnalyticsApplicationServiceExtensions).Assembly);
        });

        return services;
    }
}
