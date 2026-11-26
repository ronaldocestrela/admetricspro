using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Background;

namespace WebApi.Extensions;

/// <summary>
/// Registro condicional do job one-shot de cópia do volume orgânico (Fase 5).
/// </summary>
public static class OrganicMediaVolumeMigrationExtensions
{
    /// <summary>
    /// Registra <see cref="OrganicMediaVolumeMigrationHostedService"/> quando MinIO e <c>MigrateLocalRoot</c> estão ativos.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A coleção para encadeamento.</returns>
    public static IServiceCollection AddOrganicMediaVolumeMigration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>()
                      ?? new ObjectStorageOptions();

        if (ObjectStorageOptions.ShouldRunOrganicMediaVolumeMigration(options))
        {
            services.AddHostedService<OrganicMediaVolumeMigrationHostedService>();
        }

        return services;
    }
}