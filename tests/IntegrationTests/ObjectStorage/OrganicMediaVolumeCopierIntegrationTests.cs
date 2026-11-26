using BuildingBlocks.Application.Storage;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Options;
using SocialPublishing.Infrastructure.Media;

namespace IntegrationTests.ObjectStorage;

/// <summary>
/// Cópia do volume local para MinIO real (Fase 5).
/// </summary>
[Collection(MinioCollection.Name)]
public sealed class OrganicMediaVolumeCopierIntegrationTests
{
    private readonly MinioFixture _fixture;

    /// <summary>
    /// Inicializa com o fixture compartilhado de MinIO.
    /// </summary>
    /// <param name="fixture">Container MinIO.</param>
    public OrganicMediaVolumeCopierIntegrationTests(MinioFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Dois arquivos copiados; segunda execução ignora; leitura devolve os mesmos bytes.
    /// </summary>
    [Fact]
    public async Task CopyFromLocalVolume_PutSkipAndReadMatch()
    {
        var tenantId = Guid.Parse("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
        var workspaceId = Guid.Parse("ffffffffffffffffffffffffffffffff");
        var asset1 = Guid.Parse("11111111111111111111111111111111");
        var asset2 = Guid.Parse("22222222222222222222222222222222");

        var tempRoot = Path.Combine(Path.GetTempPath(), "organic-minio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var key1 = $"organic-media/durable/{tenantId:N}/{workspaceId:N}/{asset1:N}";
            var key2 = $"organic-media/durable/{tenantId:N}/{workspaceId:N}/{asset2:N}";
            var payload1 = new byte[] { 10, 11, 12 };
            var payload2 = new byte[] { 20, 21 };

            await WriteFile(tempRoot, key1, payload1);
            await WriteFile(tempRoot, key2, payload2);

            var accessor = new TenantContextAccessor
            {
                TenantContext = TenantContext.Create(tenantId, null, BuildingBlocks.Application.MultiTenancy.TenantResolutionSource.Header),
            };

            var store = ObjectStorageTestSupport.CreateMinioObjectStoreForTest(
                new ObjectStorageOptions
                {
                    Provider = "MinIO",
                    ServiceUrl = _fixture.ServiceUrl,
                    AccessKey = _fixture.AccessKey,
                    SecretKey = _fixture.SecretKey,
                    Region = "us-east-1",
                    OrganicBucket = _fixture.TestBucket,
                },
                accessor,
                NullLogger<MinioObjectStore>.Instance);

            var copier = new OrganicMediaVolumeCopier(
                store,
                Options.Create(new ObjectStorageOptions { OrganicBucket = _fixture.TestBucket }),
                Options.Create(new OrganicMediaOptions { KeyPrefix = "organic-media" }),
                accessor,
                NullLogger<OrganicMediaVolumeCopier>.Instance);

            var first = await copier.CopyFromLocalVolumeAsync(tempRoot, _ => "image/jpeg");
            first.Value.Copied.Should().Be(2);

            var second = await copier.CopyFromLocalVolumeAsync(tempRoot, _ => "image/jpeg");
            second.Value.Skipped.Should().Be(2);
            second.Value.Copied.Should().Be(0);

            await AssertBytes(store, _fixture.TestBucket, key1, payload1);
            await AssertBytes(store, _fixture.TestBucket, key2, payload2);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static async Task WriteFile(string root, string key, byte[] content)
    {
        var path = Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content);
    }

    private static async Task AssertBytes(IObjectStore store, string bucket, string key, byte[] expected)
    {
        var read = await store.OpenReadAsync(bucket, key);
        read.IsSuccess.Should().BeTrue();
        await using var stream = read.Value;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.ToArray().Should().Equal(expected);
    }
}