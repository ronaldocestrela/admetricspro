using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Comportamento de <see cref="LocalObjectStore"/> sem rede.
/// </summary>
public sealed class LocalObjectStoreTests : IDisposable
{
    private readonly string _root;
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    private readonly LocalObjectStore _store;

    public LocalObjectStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "admetrics-object-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(_tenantId, null, TenantResolutionSource.Header),
        };

        _store = new LocalObjectStore(
            Options.Create(new ObjectStorageOptions { LocalRoot = _root }),
            accessor,
            NullLogger<LocalObjectStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task PutAsync_ThenOpenRead_ReturnsSameBytes()
    {
        var bucket = "admetricspro-organic";
        var key = $"{_tenantId:N}/probe";
        var payload = new byte[] { 10, 20, 30 };

        (await _store.PutAsync(bucket, key, new MemoryStream(payload), "application/octet-stream")).IsSuccess.Should().BeTrue();

        var read = await _store.OpenReadAsync(bucket, key);
        read.IsSuccess.Should().BeTrue();
        await using var stream = read.Value;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.ToArray().Should().Equal(payload);
    }

    [Fact]
    public async Task PutAsync_SecondPut_ReplacesContent()
    {
        var bucket = "b";
        var key = $"{_tenantId:N}/k";

        await _store.PutAsync(bucket, key, new MemoryStream([1]), "text/plain");
        await _store.PutAsync(bucket, key, new MemoryStream([9, 9]), "image/png");

        var read = await _store.OpenReadAsync(bucket, key);
        using var ms = new MemoryStream();
        await read.Value.CopyToAsync(ms);
        ms.ToArray().Should().Equal([9, 9]);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_IsSuccess()
    {
        (await _store.DeleteAsync("bucket", $"{_tenantId:N}/missing")).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ThenOpenRead_ReturnsObjectNotFound()
    {
        const string bucket = "b";
        var key = $"{_tenantId:N}/k";

        await _store.PutAsync(bucket, key, new MemoryStream([1]), "application/octet-stream");
        (await _store.DeleteAsync(bucket, key)).IsSuccess.Should().BeTrue();
        (await _store.DeleteAsync(bucket, key)).IsSuccess.Should().BeTrue();

        var read = await _store.OpenReadAsync(bucket, key);
        read.IsFailure.Should().BeTrue();
        read.Error.Should().Be(StorageErrors.ObjectNotFound);
    }

    [Fact]
    public async Task PutAsync_WhenKeyTraversal_ReturnsKeyInvalid_AndDoesNotWriteOutsideRoot()
    {
        var maliciousKey = $"{_tenantId:N}/../escape";

        var put = await _store.PutAsync("bucket", maliciousKey, new MemoryStream([1]), "text/plain");

        put.IsFailure.Should().BeTrue();
        put.Error.Should().Be(StorageErrors.KeyInvalid);
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task PutAsync_WhenOtherTenantKey_ReturnsKeyInvalid()
    {
        var other = Guid.NewGuid();
        var key = $"{other:N}/file";

        var put = await _store.PutAsync("bucket", key, new MemoryStream([1]), "text/plain");

        put.IsFailure.Should().BeTrue();
        put.Error.Should().Be(StorageErrors.KeyInvalid);
    }

    [Fact]
    public async Task PutAsync_WhenTenantNotResolved_ReturnsKeyInvalid()
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Empty,
        };
        var store = new LocalObjectStore(
            Options.Create(new ObjectStorageOptions { LocalRoot = _root }),
            accessor,
            NullLogger<LocalObjectStore>.Instance);

        var put = await store.PutAsync("bucket", $"{_tenantId:N}/x", new MemoryStream([1]), "text/plain");

        put.IsFailure.Should().BeTrue();
        put.Error.Should().Be(StorageErrors.KeyInvalid);
    }
}