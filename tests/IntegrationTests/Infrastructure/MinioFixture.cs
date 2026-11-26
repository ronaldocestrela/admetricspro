using DotNet.Testcontainers.Configurations;
using Testcontainers.Minio;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// MinIO container fixture for object storage integration tests (Fase 3).
/// </summary>
public sealed class MinioFixture : IAsyncLifetime
{
    private const string TestBucketName = "admetricspro-integration-test";

    // minio/minio saiu do Docker Hub. bitnamilegacy/minio:2024.12.18 ainda publica o mesmo binário.
    // O entrypoint da imagem é um script e /data não é gravável pelo usuário 1001.
    // WithCommand acrescenta argumentos; OverwriteEnumerable substitui o "server /data" do módulo.
    private readonly MinioContainer _container = new MinioBuilder("bitnamilegacy/minio:2024.12.18")
        .WithEntrypoint("/opt/bitnami/minio/bin/minio")
        .WithCommand(new OverwriteEnumerable<string>(["server", "/tmp/data"]))
        .WithUsername("admetrics")
        .WithPassword("ChangeMeMinioTestOnly!")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// Obtém o endpoint HTTP da API S3 (host + porta mapeada).
    /// </summary>
    public string ServiceUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém a access key do container.
    /// </summary>
    public string AccessKey => _container.GetAccessKey();

    /// <summary>
    /// Obtém a secret key do container.
    /// </summary>
    public string SecretKey => _container.GetSecretKey();

    /// <summary>
    /// Bucket criado pelo fixture para cenários felizes.
    /// </summary>
    public string TestBucket => TestBucketName;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var port = _container.GetMappedPublicPort(9000);
        ServiceUrl = $"http://{_container.Hostname}:{port}";
        await BuildingBlocks.Infrastructure.Storage.ObjectStorageTestSupport.EnsureBucketExistsAsync(
            ServiceUrl,
            AccessKey,
            SecretKey,
            TestBucketName);
    }

    /// <inheritdoc />
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// Coleção xUnit dedicada a testes que sobem MinIO.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MinioCollection : ICollectionFixture<MinioFixture>
{
    /// <summary>
    /// Nome da coleção.
    /// </summary>
    public const string Name = "MinioCollection";
}
