using Automations.Application.Persistence;
using Automations.Application.Rules.Services;
using Automations.Domain.Rules;
using Automations.Infrastructure.Persistence;
using Automations.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automations.Infrastructure.DependencyInjection;

/// <summary>
/// Extensões de injeção de dependência para a camada de infraestrutura do módulo Automations.
/// </summary>
public static class AutomationsInfrastructureExtensions
{
    /// <summary>
    /// Registra repositórios, persistência e serviços do módulo Automations.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A mesma coleção para encadeamento.</returns>
    public static IServiceCollection AddAutomationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
        services.AddScoped<IAutomationsUnitOfWork, AutomationsUnitOfWork>();
        services.AddScoped<IAutomationsMetricsProvider, AutomationsMetricsProvider>();

        return services;
    }
}
