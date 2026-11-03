using Master.Application.Services;
using Master.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialPublishing.Application.Abstractions;
using WebApi.Infrastructure;

namespace WebApi.Extensions;

/// <summary>
/// Extensões de composição na raiz WebApi para persistência SocialPublishing em tenants.
/// </summary>
public static class SocialPublishingCompositionExtensions
{
    /// <summary>
    /// Substitui o registro de <see cref="ITenantProvisioningService"/> por um decorator que aplica migrações sociais.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>Coleção para encadeamento.</returns>
    public static IServiceCollection AddSocialPublishingTenantPersistenceComposition(this IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITenantProvisioningService));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.TryAddScoped<TenantProvisioningService>();
        services.AddScoped<ITenantProvisioningService, SocialPublishingTenantProvisioningDecorator>();
        services.AddHostedService<Background.SocialPublishingTenantMigrationHostedService>();

        return services;
    }
}
