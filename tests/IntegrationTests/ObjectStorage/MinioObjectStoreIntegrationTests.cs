using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationTests.ObjectStorage;

/// <summary>
/// Put/Get/Delete reais contra MinIO (única suíte que sobe container nesta fase).
/// </summary>
[Collection(MinioCollection.Name)]
public sealed class MinioObjectStoreIntegrationTests
{
    private readonly MinioFixture _fixture;

    /// <summary>
    /// Inicializa com o fixture compartilhado de MinIO.
    /// </summary>
    /// <param name="fixture">Container MinIO.</param>
    public MinioObjectStoreIntegrationTests(MinioFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Ciclo put, leitura, exclusão e objeto ausente após delete.
    /// </summary>
    [Fact]
    public async Task Put_OpenRead_Delete_ThenOpenRead_ReturnsObjectNotFound()
    {
        var tenantId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var store = CreateStore(tenantId, NullLogger<MinioObjectStore>.Instance);
        var bucket = _fixture.TestBucket;
        var key = $"{tenantId:N}/probe";
        var payload = new byte[] { 1, 2, 3, 4, 5 };

        (await store.PutAsync(bucket, key, new MemoryStream(payload), "application/octet-stream")).IsSuccess.Should().BeTrue();

        var read = await store.OpenReadAsync(bucket, key);
        read.IsSuccess.Should().BeTrue();
        await using var stream = read.Value;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.ToArray().Should().Equal(payload);

        (await store.DeleteAsync(bucket, key)).IsSuccess.Should().BeTrue();

        var afterDelete = await store.OpenReadAsync(bucket, key);
        afterDelete.IsFailure.Should().BeTrue();
        afterDelete.Error.Code.Should().Be("Storage.ObjectNotFound");
        afterDelete.Error.Description.Should().Be(StorageErrors.ObjectNotFound.Description);
    }

    /// <summary>
    /// Exclusão idempotente quando a chave já foi removida.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenAlreadyDeleted_IsSuccess()
    {
        var tenantId = Guid.Parse("cccccccccccccccccccccccccccccccc");
        var store = CreateStore(tenantId, NullLogger<MinioObjectStore>.Instance);
        var bucket = _fixture.TestBucket;
        var key = $"{tenantId:N}/idempotent-delete";

        await store.PutAsync(bucket, key, new MemoryStream([1]), "application/octet-stream");
        (await store.DeleteAsync(bucket, key)).IsSuccess.Should().BeTrue();
        (await store.DeleteAsync(bucket, key)).IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Bucket inexistente retorna erro estável sem vazar mensagem do SDK ao cliente.
    /// </summary>
    [Fact]
    public async Task PutAsync_WhenBucketMissing_ReturnsWriteFailed_WithoutSdkMessageInDescription()
    {
        var tenantId = Guid.Parse("dddddddddddddddddddddddddddddddd");
        var collectingLogger = new CollectingLogger<MinioObjectStore>();
        var store = CreateStore(tenantId, collectingLogger);
        const string missingBucket = "admetricspro-bucket-that-was-never-created";
        var key = $"{tenantId:N}/probe";

        var put = await store.PutAsync(missingBucket, key, new MemoryStream([9]), "application/octet-stream");

        put.IsFailure.Should().BeTrue();
        put.Error.Code.Should().Be("Storage.WriteFailed");
        put.Error.Description.Should().Be(StorageErrors.WriteFailed.Description);
        put.Error.Description.Should().NotContain(missingBucket);
        put.Error.Description.Should().NotContain(_fixture.ServiceUrl);
        put.Error.Description.Should().NotContain("NoSuchBucket");

        collectingLogger.Entries.Should().NotBeEmpty();
        var combined = string.Join(' ', collectingLogger.Entries.Select(e => e.Message));
        combined.Should().Contain(_fixture.ServiceUrl);
        collectingLogger.Entries.Should().NotContain(e => e.Message.Contains('9') && e.Message.Contains("byte"));
    }

    private MinioObjectStore CreateStore(Guid tenantId, ILogger<MinioObjectStore> logger)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(tenantId, null, TenantResolutionSource.Header),
        };

        var options = new ObjectStorageOptions
        {
            Provider = "MinIO",
            ServiceUrl = _fixture.ServiceUrl,
            AccessKey = _fixture.AccessKey,
            SecretKey = _fixture.SecretKey,
            Region = "us-east-1",
        };

        return ObjectStorageTestSupport.CreateMinioObjectStoreForTest(options, accessor, logger);
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}