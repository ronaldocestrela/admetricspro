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
        services.AddScoped<Automations.Domain.SafetyGuards.ISafetyGuardIncidentRepository, Automations.Infrastructure.Persistence.SafetyGuardIncidentRepository>();

        // Travas de Segurança Operacional & Notificações Multi-Canal (Subfase 4.2)
        services.Configure<Automations.Infrastructure.SafetyGuards.SecurityAlertNotificationOptions>(
            configuration.GetSection(Automations.Infrastructure.SafetyGuards.SecurityAlertNotificationOptions.SectionName));

        services.AddHttpClient<Automations.Domain.SafetyGuards.ISlackWebhookNotifier, Automations.Infrastructure.SafetyGuards.SlackWebhookNotifier>();
        services.AddHttpClient<Automations.Domain.SafetyGuards.IWhatsAppWebhookNotifier, Automations.Infrastructure.SafetyGuards.WhatsAppWebhookNotifier>();
        services.AddHttpClient<Automations.Domain.SafetyGuards.IGenericWebhookNotifier, Automations.Infrastructure.SafetyGuards.GenericWebhookNotifier>();
        services.AddHttpClient<Automations.Domain.SafetyGuards.IHttpLandingPageVerifier, Automations.Infrastructure.SafetyGuards.HttpLandingPageVerifier>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddScoped<Automations.Domain.SafetyGuards.IEmailAlertNotifier, Automations.Infrastructure.SafetyGuards.EmailAlertNotifier>();
        services.AddScoped<Automations.Domain.SafetyGuards.ISecurityAlertNotifier, Automations.Infrastructure.SafetyGuards.SecurityAlertDispatcher>();
        services.AddScoped<Automations.Domain.SafetyGuards.IOverspendingGuard, Automations.Infrastructure.SafetyGuards.OverspendingGuard>();
        services.AddScoped<Automations.Domain.SafetyGuards.ILandingPageHealthChecker, Automations.Infrastructure.SafetyGuards.LandingPageHealthChecker>();

        return services;
    }
}
