using Analytics.Domain.Attribution;
using Analytics.Domain.Blended;
using Analytics.Domain.Currencies;
using Analytics.Domain.Taxonomy;
using Analytics.Infrastructure.Attribution;
using Analytics.Infrastructure.Blended;
using Analytics.Infrastructure.Currencies;
using Analytics.Infrastructure.Taxonomy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Analytics.Infrastructure.DependencyInjection;

/// <summary>
/// Métodos de extensão para registro de dependências da camada de infraestrutura do módulo Analytics.
/// </summary>
public static class AnalyticsInfrastructureServiceExtensions
{
    /// <summary>
    /// Registra os serviços de infraestrutura analítica (conversor cambial, provedor de cotações, cache, taxonomia, blended metrics e atribuição).
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddAnalyticsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Registro do cache em memória para cotações
        services.AddMemoryCache();

        // Serviços cambiais
        services.AddSingleton<IExchangeRateProvider, CanonicalExchangeRateProvider>();
        services.AddSingleton<ICurrencyConverter, CurrencyConverter>();

        // Motor de taxonomia
        services.AddSingleton<ITaxonomyClassifier, AutomatedTaxonomyClassifier>();

        // Motores de Blended Metrics e Atribuição Multicanal
        services.AddScoped<IBlendedMetricsCalculator, BlendedMetricsCalculator>();
        services.AddScoped<IAttributionCalculator, AttributionCalculator>();

        return services;
    }
}
