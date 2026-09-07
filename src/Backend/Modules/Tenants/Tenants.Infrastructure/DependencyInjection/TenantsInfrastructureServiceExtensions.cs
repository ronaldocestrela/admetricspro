using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenants.Application.Auth.Services;
using Tenants.Infrastructure.Auth;

namespace Tenants.Infrastructure.DependencyInjection;

/// <summary>
/// Métodos de extensão de injeção de dependência para registro dos serviços e configurações do módulo Tenants.Infrastructure.
/// </summary>
public static class TenantsInfrastructureServiceExtensions
{
    /// <summary>
    /// Registra os serviços de infraestrutura de inquilinos, incluindo autenticação e gerador de JWT.
    /// </summary>
    /// <param name="services">A coleção de serviços da aplicação.</param>
    /// <param name="configuration">Instância de configuração para binding de opções.</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddTenantsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TenantJwtOptions>(options =>
        {
            configuration.GetSection(TenantJwtOptions.SectionName).Bind(options);
        });

        services.AddScoped<ITenantAuthService, TenantAuthService>();
        services.AddScoped<ITenantTokenService, TenantJwtTokenService>();

        return services;
    }
}
