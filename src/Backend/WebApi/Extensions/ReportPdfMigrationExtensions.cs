using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Background;

namespace WebApi.Extensions;

/// <summary>
/// Registro condicional do job one-shot de cópia de PDFs de relatório (Fase 6).
/// </summary>
public static class ReportPdfMigrationExtensions
{
    /// <summary>
    /// Registra <see cref="ReportPdfMigrationHostedService"/> quando MinIO e <c>MigrateReportPdfs</c> estão ativos.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A coleção para encadeamento.</returns>
    public static IServiceCollection AddReportPdfMigration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>()
                      ?? new ObjectStorageOptions();

        if (ObjectStorageOptions.ShouldRunReportPdfMigration(options))
        {
            services.AddHostedService<ReportPdfMigrationHostedService>();
        }

        return services;
    }
}