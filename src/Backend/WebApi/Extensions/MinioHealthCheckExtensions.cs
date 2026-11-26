using Microsoft.Extensions.Diagnostics.HealthChecks;
using BuildingBlocks.Infrastructure.Storage;
using WebApi.HealthChecks;

namespace WebApi.Extensions;

/// <summary>
/// Registra o health check nomeado <c>minio</c> quando <see cref="ObjectStorageOptions.Provider"/> é <c>MinIO</c>.
/// </summary>
public static class MinioHealthCheckExtensions
{
    private const string HealthCheckName = "minio";

    /// <summary>
    /// Adiciona o probe HTTP ao pipeline de health checks somente se o provedor de object storage for MinIO.
    /// </summary>
    /// <param name="builder">Builder retornado por <c>AddHealthChecks()</c>.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>O mesmo <paramref name="builder"/> para encadeamento.</returns>
    public static IHealthChecksBuilder AddMinioHealthCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>()
                      ?? new ObjectStorageOptions();

        if (!ObjectStorageOptions.ShouldRegisterMinioHealthCheck(options))
        {
            return builder;
        }

        var serviceUrl = options.ServiceUrl;

        builder.Services.AddHttpClient(MinioHealthCheckHttpClientRegistration.HttpClientName, client =>
        {
            client.Timeout = MinioHealthCheckHttpClientRegistration.HealthCheckTimeout;
        });

        builder.Services.AddSingleton<MinioLiveHealthCheck>(sp =>
            new MinioLiveHealthCheck(
                sp.GetRequiredService<IHttpClientFactory>(),
                serviceUrl));

        return builder.AddCheck<MinioLiveHealthCheck>(
            HealthCheckName,
            failureStatus: HealthStatus.Unhealthy,
            tags: null,
            timeout: MinioHealthCheckHttpClientRegistration.HealthCheckTimeout);
    }
}