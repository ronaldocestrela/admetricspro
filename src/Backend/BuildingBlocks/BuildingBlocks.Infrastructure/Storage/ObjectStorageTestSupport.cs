using Amazon.S3;
using Amazon.S3.Model;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Storage;

/// <summary>
/// Auxiliares usados por testes de integração com MinIO (não exposto a módulos de produto).
/// </summary>
public static class ObjectStorageTestSupport
{
    /// <summary>
    /// Cria um cliente S3 apontando para o endpoint de teste.
    /// </summary>
    /// <param name="options">Opções com URL e credenciais.</param>
    /// <returns>Cliente configurado com path-style.</returns>
    public static IAmazonS3 CreateAmazonS3Client(ObjectStorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = options.Region,
        };

        return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }

    /// <summary>
    /// Garante que o bucket existe (idempotente).
    /// </summary>
    /// <param name="serviceUrl">Endpoint HTTP da API S3.</param>
    /// <param name="accessKey">Access key.</param>
    /// <param name="secretKey">Secret key.</param>
    /// <param name="bucketName">Nome do bucket.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa concluída quando o bucket existir.</returns>
    public static async Task EnsureBucketExistsAsync(
        string serviceUrl,
        string accessKey,
        string secretKey,
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        var options = new ObjectStorageOptions
        {
            ServiceUrl = serviceUrl,
            AccessKey = accessKey,
            SecretKey = secretKey,
            Region = "us-east-1",
        };

        using var client = CreateAmazonS3Client(options);

        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (IsBucketAlreadyExists(ex))
        {
            // Idempotente.
        }
    }

    private static bool IsBucketAlreadyExists(AmazonS3Exception ex) =>
        string.Equals(ex.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.Ordinal)
        || string.Equals(ex.ErrorCode, "BucketAlreadyExists", StringComparison.Ordinal);

    /// <summary>
    /// Monta <see cref="MinioObjectStore"/> para testes de integração sem expor <see cref="IAmazonS3"/> ao projeto de teste.
    /// </summary>
    /// <param name="options">Opções com endpoint e credenciais.</param>
    /// <param name="tenantContextAccessor">Acessor de tenant.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Store configurado.</returns>
    public static MinioObjectStore CreateMinioObjectStoreForTest(
        ObjectStorageOptions options,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<MinioObjectStore> logger) =>
        new(
            CreateAmazonS3Client(options),
            Options.Create(options),
            tenantContextAccessor,
            logger);
}