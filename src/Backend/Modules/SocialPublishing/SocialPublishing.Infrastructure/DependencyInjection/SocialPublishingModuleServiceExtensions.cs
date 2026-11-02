using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SocialPublishing.Infrastructure.DependencyInjection;

/// <summary>
/// Registro de composição do bounded context SocialPublishing na raiz WebApi.
/// </summary>
public static class SocialPublishingModuleServiceExtensions
{
    /// <summary>
    /// Registra MediatR, validadores FluentValidation e behaviors do módulo SocialPublishing.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação (reservada para opções futuras do módulo).</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddSocialPublishingModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMessaging(Assembly.Load(new AssemblyName("SocialPublishing.Application")));

        return services;
    }
}
