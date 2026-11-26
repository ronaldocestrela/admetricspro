using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;
using SocialPublishing.Infrastructure.Media;
using UnitTests.Backend.ObjectStorage;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Cópia do volume local de mídia orgânica para <see cref="IObjectStore"/> (Fase 5).
/// </summary>
public sealed class OrganicMediaVolumeCopierTests : IDisposable
{
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    private readonly Guid _workspaceId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
    private readonly Guid _assetId1 = Guid.Parse("cccccccccccccccccccccccccccccccc");
    private readonly Guid _assetId2 = Guid.Parse("dddddddddddddddddddddddddddddddd");
    private readonly string _tempRoot;
    private readonly TenantContextAccessor _tenantAccessor = new();

    public OrganicMediaVolumeCopierTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "organic-volume-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task CopyFromLocalVolume_FirstRun_CopiesTwoFiles()
    {
        var key1 = BuildKey(_assetId1);
        var key2 = BuildKey(_assetId2);
        await File.WriteAllBytesAsync(PathFor(key1), [1, 2, 3]);
        await File.WriteAllBytesAsync(PathFor(key2), [4, 5]);

        var store = new InMemoryObjectStore();
        var copier = CreateCopier(store);

        var result = await copier.CopyFromLocalVolumeAsync(_tempRoot, _ => "image/jpeg");

        result.IsSuccess.Should().BeTrue();
        result.Value.Copied.Should().Be(2);
        result.Value.Skipped.Should().Be(0);
        result.Value.Failed.Should().Be(0);
        store.Keys.Should().HaveCount(2);

        var read = await store.OpenReadAsync("admetricspro-organic", key1);
        using var ms = new MemoryStream();
        await read.Value.CopyToAsync(ms);
        ms.ToArray().Should().Equal([1, 2, 3]);
    }

    [Fact]
    public async Task CopyFromLocalVolume_SecondRun_SkipsWhenSameSize()
    {
        var key = BuildKey(_assetId1);
        await File.WriteAllBytesAsync(PathFor(key), [9, 9, 9]);

        var store = new InMemoryObjectStore();
        var copier = CreateCopier(store);

        (await copier.CopyFromLocalVolumeAsync(_tempRoot, _ => "image/png")).Value.Copied.Should().Be(1);
        var second = await copier.CopyFromLocalVolumeAsync(_tempRoot, _ => "image/png");

        second.Value.Copied.Should().Be(0);
        second.Value.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task CopyFromLocalVolume_WhenRemoteSizeDiffers_ReplacesAndCountsCopied()
    {
        var key = BuildKey(_assetId1);
        await File.WriteAllBytesAsync(PathFor(key), [1, 2, 3, 4]);

        var store = new InMemoryObjectStore();
        await store.PutAsync("admetricspro-organic", key, new MemoryStream([1]), "image/jpeg");

        var copier = CreateCopier(store);
        var result = await copier.CopyFromLocalVolumeAsync(_tempRoot, _ => "image/jpeg");

        result.Value.Copied.Should().Be(1);
        result.Value.Skipped.Should().Be(0);

        var read = await store.OpenReadAsync("admetricspro-organic", key);
        using var ms = new MemoryStream();
        await read.Value.CopyToAsync(ms);
        ms.ToArray().Should().Equal([1, 2, 3, 4]);
    }

    [Fact]
    public async Task CopyFromLocalVolume_WhenPutFails_CountsFailedAndContinues()
    {
        var key1 = BuildKey(_assetId1);
        var key2 = BuildKey(_assetId2);
        await File.WriteAllBytesAsync(PathFor(key1), [1]);
        await File.WriteAllBytesAsync(PathFor(key2), [2]);

        var store = new SelectiveFailObjectStore(key1);
        var copier = CreateCopier(store);

        var result = await copier.CopyFromLocalVolumeAsync(_tempRoot, _ => "image/jpeg");

        result.Value.Copied.Should().Be(1);
        result.Value.Failed.Should().Be(1);
        store.InnerPutCount.Should().Be(2);
    }

    [Fact]
    public async Task CopyFromLocalVolume_InvalidKeyLayout_CountsFailedWithoutPut()
    {
        var badPath = Path.Combine(_tempRoot, "not-organic-layout.bin");
        await File.WriteAllBytesAsync(badPath, [1]);

        var store = new NoPutObjectStore();
        var copier = CreateCopier(store);

        var result = await copier.CopyFromLocalVolumeAsync(_tempRoot, _ => "application/octet-stream");

        result.Value.Failed.Should().Be(1);
        result.Value.Copied.Should().Be(0);
        store.PutInvoked.Should().BeFalse();
    }

    [Fact]
    public void CountMissingOnDisk_WhenSqlKeyHasNoFile_IncrementsMissing()
    {
        var key = BuildKey(_assetId1);
        var copier = CreateCopier(new InMemoryObjectStore());

        var missing = copier.CountMissingOnDisk(
            [new OrganicMediaCatalogEntry(key, "image/jpeg")],
            _tempRoot);

        missing.Should().Be(1);
    }

    private OrganicMediaVolumeCopier CreateCopier(IObjectStore store) =>
        new(
            store,
            Options.Create(new ObjectStorageOptions { OrganicBucket = "admetricspro-organic" }),
            Options.Create(new OrganicMediaOptions { KeyPrefix = "organic-media" }),
            _tenantAccessor,
            NullLogger<OrganicMediaVolumeCopier>.Instance);

    private string BuildKey(Guid assetId) =>
        $"organic-media/durable/{_tenantId:N}/{_workspaceId:N}/{assetId:N}";

    private string PathFor(string key)
    {
        var path = Path.Combine(_tempRoot, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    private sealed class NoPutObjectStore : IObjectStore
    {
        private readonly InMemoryObjectStore _inner = new();

        public bool PutInvoked { get; private set; }

        public Task<Result> PutAsync(
            string bucket,
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            PutInvoked = true;
            return _inner.PutAsync(bucket, key, content, contentType, cancellationToken);
        }

        public Task<Result<Stream>> OpenReadAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.OpenReadAsync(bucket, key, cancellationToken);

        public Task<Result> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(bucket, key, cancellationToken);

        public Task<Result<bool>> ExistsAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.ExistsAsync(bucket, key, cancellationToken);

        public Task<Result<long?>> GetContentLengthAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.GetContentLengthAsync(bucket, key, cancellationToken);
    }

    private sealed class SelectiveFailObjectStore : IObjectStore
    {
        private readonly InMemoryObjectStore _inner = new();
        private readonly string _failKey;

        public SelectiveFailObjectStore(string failKey) => _failKey = failKey;

        public int InnerPutCount { get; private set; }

        public Task<Result> PutAsync(
            string bucket,
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            InnerPutCount++;
            if (string.Equals(key, _failKey, StringComparison.Ordinal))
            {
                return Task.FromResult(Result.Failure(StorageErrors.WriteFailed));
            }

            return _inner.PutAsync(bucket, key, content, contentType, cancellationToken);
        }

        public Task<Result<Stream>> OpenReadAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.OpenReadAsync(bucket, key, cancellationToken);

        public Task<Result> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(bucket, key, cancellationToken);

        public Task<Result<bool>> ExistsAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.ExistsAsync(bucket, key, cancellationToken);

        public Task<Result<long?>> GetContentLengthAsync(string bucket, string key, CancellationToken cancellationToken = default) =>
            _inner.GetContentLengthAsync(bucket, key, cancellationToken);
    }
}