using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Infrastructure.Media;
using UnitTests.Backend.ObjectStorage;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes de paridade de chaves entre store local e adaptador <see cref="IObjectStore"/>.
/// </summary>
public sealed class OrganicMediaStoreTests
{
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    private readonly Guid _workspaceId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
    private readonly Guid _assetId = Guid.Parse("cccccccccccccccccccccccccccccccc");
    private readonly Guid _otherTenantId = Guid.Parse("dddddddddddddddddddddddddddddddd");

    /// <summary>
    /// Local e adaptador devem produzir a mesma chave durable.
    /// </summary>
    [Fact]
    public async Task SaveAsync_LocalEObjectStore_ProduzemMesmaChaveDurable()
    {
        var options = Options.Create(new OrganicMediaOptions { KeyPrefix = "organic-media", LocalRoot = Path.GetTempPath() });
        var localStore = new LocalOrganicMediaStore(options, NullLogger<LocalOrganicMediaStore>.Instance);
        var objectStore = CreateObjectStoreAdapter(_tenantId, new InMemoryObjectStore());

        await using var stream = new MemoryStream([1, 2, 3]);
        var localKey = await localStore.SaveAsync(
            _tenantId, _workspaceId, _assetId, stream, "image/jpeg", OrganicMediaStorageTier.Durable, CancellationToken.None);

        await using var stream2 = new MemoryStream([1, 2, 3]);
        var objectKey = await objectStore.SaveAsync(
            _tenantId, _workspaceId, _assetId, stream2, "image/jpeg", OrganicMediaStorageTier.Durable, CancellationToken.None);

        var expected = $"organic-media/durable/{_tenantId:N}/{_workspaceId:N}/{_assetId:N}";
        localKey.Value.Should().Be(expected);
        objectKey.Value.Should().Be(expected);
    }

    /// <summary>
    /// Adaptador recusa gravação com tenant divergente do contexto.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ComTenantDivergente_DeveRetornarKeyInvalid()
    {
        var fake = new InMemoryObjectStore();
        var store = CreateObjectStoreAdapter(_tenantId, fake);

        await using var stream = new MemoryStream([1]);
        var result = await store.SaveAsync(
            _otherTenantId, _workspaceId, _assetId, stream, "image/jpeg", OrganicMediaStorageTier.Staging, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StorageErrors.KeyInvalid.Code);
        var exists = await fake.ExistsAsync("admetricspro-organic", $"organic-media/staging/{_otherTenantId:N}/{_workspaceId:N}/{_assetId:N}", CancellationToken.None);
        exists.Value.Should().BeFalse();
    }

    /// <summary>
    /// OpenRead de chave de outro tenant não deve chamar o fake quando o contexto é tenant A.
    /// </summary>
    [Fact]
    public async Task OpenReadAsync_ChaveDeOutroTenant_DeveFalharSemChamarStore()
    {
        var inner = new InMemoryObjectStore();
        var tracking = new TrackingObjectStore(inner);
        var store = CreateObjectStoreAdapter(_tenantId, tracking);
        var foreignKey = $"organic-media/durable/{_otherTenantId:N}/{_workspaceId:N}/{_assetId:N}";

        var result = await store.OpenReadAsync(foreignKey, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StorageErrors.KeyInvalid.Code);
        tracking.OpenReadCalls.Should().Be(0);
    }

    private ObjectStoreOrganicMediaStore CreateObjectStoreAdapter(Guid tenantId, IObjectStore inner)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(tenantId, "slug", TenantResolutionSource.Header),
        };

        return new ObjectStoreOrganicMediaStore(
            inner,
            Options.Create(new ObjectStorageOptions { OrganicBucket = "admetricspro-organic" }),
            Options.Create(new OrganicMediaOptions { KeyPrefix = "organic-media" }),
            accessor);
    }

    private sealed class TrackingObjectStore(IObjectStore inner) : IObjectStore
    {
        public int OpenReadCalls { get; private set; }

        public Task<BuildingBlocks.Domain.Primitives.Result> PutAsync(
            string bucket,
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default) =>
            inner.PutAsync(bucket, key, content, contentType, cancellationToken);

        public Task<BuildingBlocks.Domain.Primitives.Result<Stream>> OpenReadAsync(
            string bucket,
            string key,
            CancellationToken cancellationToken = default)
        {
            OpenReadCalls++;
            return inner.OpenReadAsync(bucket, key, cancellationToken);
        }

        public Task<BuildingBlocks.Domain.Primitives.Result> DeleteAsync(
            string bucket,
            string key,
            CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(bucket, key, cancellationToken);

        public Task<BuildingBlocks.Domain.Primitives.Result<bool>> ExistsAsync(
            string bucket,
            string key,
            CancellationToken cancellationToken = default) =>
            inner.ExistsAsync(bucket, key, cancellationToken);

        public Task<BuildingBlocks.Domain.Primitives.Result<long?>> GetContentLengthAsync(
            string bucket,
            string key,
            CancellationToken cancellationToken = default) =>
            inner.GetContentLengthAsync(bucket, key, cancellationToken);
    }
}