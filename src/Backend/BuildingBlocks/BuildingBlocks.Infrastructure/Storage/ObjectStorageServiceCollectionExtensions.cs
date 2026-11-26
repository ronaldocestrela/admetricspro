using Amazon.S3;
using BuildingBlocks.Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Storage;

/// <summary>
/// Registro de <see cref="IObjectStore"/> e cliente S3 condicional (Fase 3).
/// </summary>
public static class ObjectStorageServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona armazenamento de objetos ao contêiner de DI.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A coleção para encadeamento.</returns>
    /// <exception cref="InvalidOperationException">Quando <c>ObjectStorage:Provider</c> não é <c>Local</c> nem <c>MinIO</c>.</exception>
    public static IServiceCollection AddObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));

        var options = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>()
                      ?? new ObjectStorageOptions();

        if (IsLocalProvider(options.Provider))
        {
            services.AddSingleton<IObjectStore, LocalObjectStore>();
            return services;
        }

        if (IsMinIoProvider(options.Provider))
        {
            services.AddSingleton<IAmazonS3>(sp => CreateAmazonS3Client(sp.GetRequiredService<IOptions<ObjectStorageOptions>>().Value));
            services.AddSingleton<IObjectStore, MinioObjectStore>();
            return services;
        }

        throw new InvalidOperationException(
            $"ObjectStorage:Provider '{options.Provider}' não é suportado. Use Local ou MinIO.");
    }

    private static bool IsLocalProvider(string? provider) =>
        string.IsNullOrWhiteSpace(provider)
        || string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase);

    private static bool IsMinIoProvider(string? provider) =>
        string.Equals(provider, "MinIO", StringComparison.OrdinalIgnoreCase);

    private static IAmazonS3 CreateAmazonS3Client(ObjectStorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = options.Region,
        };

        return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }
}