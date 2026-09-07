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
        services.AddScoped<Tenants.Application.Workspaces.Repositories.IWorkspaceRepository, Tenants.Infrastructure.Workspaces.WorkspaceRepository>();
        services.AddScoped<Tenants.Application.Squads.Repositories.ISquadRepository, Tenants.Infrastructure.Squads.SquadRepository>();
        services.AddScoped<Tenants.Application.Users.Repositories.ITenantUserRepository, Tenants.Infrastructure.Users.TenantUserRepository>();
        services.AddScoped<Tenants.Application.Squads.Services.IUserPortfolioService, Tenants.Infrastructure.Squads.UserPortfolioService>();
        services.AddScoped<Tenants.Application.Integrations.Repositories.IConnectedAdAccountRepository, Tenants.Infrastructure.Integrations.ConnectedAdAccountRepository>();
        services.AddScoped<Tenants.Application.Persistence.ITenantUnitOfWork, Tenants.Infrastructure.Persistence.TenantUnitOfWork>();

        return services;
    }
}
