using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Infrastructure.Persistence;
using SocialPublishing.Infrastructure.Persistence.Repositories;

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

        services.AddScoped<SocialPublishingDbContextFactory>();
        services.AddScoped<ITenantDbContextFactory<SocialPublishingDbContext>>(sp =>
            sp.GetRequiredService<SocialPublishingDbContextFactory>());
        services.AddScoped<SocialPublishingDbContextAccessor>();
        services.AddScoped<IOrganicConnectionRepository, OrganicConnectionRepository>();
        services.AddScoped<IOrganicPostRepository, OrganicPostRepository>();
        services.AddScoped<IBrandVoiceRepository, BrandVoiceRepository>();
        services.AddScoped<ISocialPublishingUnitOfWork, SocialPublishingUnitOfWork>();
        services.AddScoped<ISocialPublishingSchemaMigrator, SocialPublishingSchemaMigrator>();

        return services;
    }
}
