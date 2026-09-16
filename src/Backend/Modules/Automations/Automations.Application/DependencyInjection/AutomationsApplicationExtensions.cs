using Automations.Application.Rules.Services;
using Automations.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Automations.Application.DependencyInjection;

/// <summary>
/// Extensões de injeção de dependência para a camada de aplicação do módulo Automations.
/// </summary>
public static class AutomationsApplicationExtensions
{
    /// <summary>
    /// Registra os serviços, handlers e mediadores da camada de aplicação do módulo Automations.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A mesma coleção de serviços para encadeamento.</returns>
    public static IServiceCollection AddAutomationsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AutomationsApplicationExtensions).Assembly);
        });

        services.AddScoped<IRuleConditionEvaluator, RuleConditionEvaluator>();
        services.AddScoped<IRuleActionDispatcher, RuleActionDispatcher>();
        services.AddScoped<Automations.Domain.Pacing.IBudgetPacingCalculator, Automations.Domain.Pacing.BudgetPacingCalculator>();

        return services;
    }
}
