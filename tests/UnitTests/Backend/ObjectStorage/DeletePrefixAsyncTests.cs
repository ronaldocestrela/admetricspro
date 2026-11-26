using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Testes de <see cref="IObjectStore.DeletePrefixAsync"/>.
/// </summary>
public sealed class DeletePrefixAsyncTests
{
    private readonly Guid _tenantA = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    private readonly Guid _tenantB = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

    /// <summary>
    /// Remove apenas chaves do tenant do prefixo.
    /// </summary>
    [Fact]
    public async Task DeletePrefixAsync_ShouldRemoveOnlyMatchingTenantKeys()
    {
        var accessor = CreateAccessor(_tenantA);
        var store = new InMemoryObjectStore();

        await Put(store, "bucket", $"organic-media/durable/{_tenantA:N}/ws/asset1");
        await Put(store, "bucket", $"organic-media/durable/{_tenantB:N}/ws/asset2");

        var delete = await store.DeletePrefixAsync(
            "bucket",
            $"organic-media/durable/{_tenantA:N}/",
            CancellationToken.None);

        delete.IsSuccess.Should().BeTrue();
        (await store.ExistsAsync("bucket", $"organic-media/durable/{_tenantA:N}/ws/asset1")).Value.Should().BeFalse();
        (await store.ExistsAsync("bucket", $"organic-media/durable/{_tenantB:N}/ws/asset2")).Value.Should().BeTrue();
    }

    /// <summary>
    /// Prefixo de outro tenant retorna KeyInvalid no store local.
    /// </summary>
    [Fact]
    public async Task DeletePrefixAsync_WhenOtherTenantPrefix_ShouldReturnKeyInvalid()
    {
        var root = Path.Combine(Path.GetTempPath(), $"obj-prefix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var accessor = CreateAccessor(_tenantA);
        var store = new LocalObjectStore(
            Options.Create(new ObjectStorageOptions { LocalRoot = root }),
            accessor,
            NullLogger<LocalObjectStore>.Instance);

        var result = await store.DeletePrefixAsync(
            "bucket",
            $"organic-media/durable/{_tenantB:N}/",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StorageErrors.KeyInvalid.Code);
    }

    private static ITenantContextAccessor CreateAccessor(Guid tenantId)
    {
        var context = Substitute.For<ITenantContext>();
        context.TenantId.Returns(tenantId);
        var accessor = Substitute.For<ITenantContextAccessor>();
        accessor.TenantContext.Returns(context);
        return accessor;
    }

    private static async Task Put(InMemoryObjectStore store, string bucket, string key)
    {
        using var stream = new MemoryStream([1, 2, 3]);
        (await store.PutAsync(bucket, key, stream, "application/octet-stream")).IsSuccess.Should().BeTrue();
    }
}