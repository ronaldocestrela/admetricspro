using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media.Commands.RegisterOrganicMedia;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Infrastructure.Media;
using UnitTests.Backend.ObjectStorage;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Registro de mídia com adaptador ObjectStore e bucket em memória.
/// </summary>
public sealed class RegisterOrganicMediaObjectStoreCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// MIME inválido remove staging do bucket em memória.
    /// </summary>
    [Fact]
    public async Task Handle_ComProporcaoInvalida_RemoveStagingDoObjectStore()
    {
        var objectStore = new InMemoryObjectStore();
        var mediaStore = CreateMediaStore(objectStore);
        var handler = CreateHandler(mediaStore, metadataReader: new FixedMetadataReader(1920, 1080, null));
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await handler.Handle(
            new RegisterOrganicMediaCommand(_workspaceId, OrganicPostFormat.FeedImage, OrganicMediaMimeTypes.Jpeg, stream),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaAspectInvalid.Code);
        objectStore.Keys.Should().BeEmpty();
    }

    /// <summary>
    /// Upload válido deixa apenas chave durable no bucket.
    /// </summary>
    [Fact]
    public async Task Handle_ComMidiaValida_MantemApenasDurable()
    {
        var objectStore = new InMemoryObjectStore();
        var mediaStore = CreateMediaStore(objectStore);
        var repository = new InMemoryOrganicMediaAssetRepository();
        var handler = CreateHandler(mediaStore, repository);
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await handler.Handle(
            new RegisterOrganicMediaCommand(_workspaceId, OrganicPostFormat.FeedImage, OrganicMediaMimeTypes.Jpeg, stream),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        objectStore.Keys.Should().ContainSingle();
        objectStore.Keys.Single().Key.Should().Contain("/durable/");
        objectStore.Keys.Single().Key.Should().NotContain("/staging/");
        repository.Items.Should().ContainSingle();
    }

    private ObjectStoreOrganicMediaStore CreateMediaStore(InMemoryObjectStore objectStore)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(_tenantId, "slug", TenantResolutionSource.Header),
        };

        return new ObjectStoreOrganicMediaStore(
            objectStore,
            Options.Create(new ObjectStorageOptions { OrganicBucket = "admetricspro-organic" }),
            Options.Create(new OrganicMediaOptions()),
            accessor);
    }

    private RegisterOrganicMediaCommandHandler CreateHandler(
        IOrganicMediaStore mediaStore,
        InMemoryOrganicMediaAssetRepository? repository = null,
        IOrganicMediaMetadataReader? metadataReader = null)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(_tenantId, "slug", TenantResolutionSource.Header),
        };

        return new RegisterOrganicMediaCommandHandler(
            accessor,
            mediaStore,
            metadataReader ?? new FixedMetadataReader(1080, 1080, null),
            repository ?? new InMemoryOrganicMediaAssetRepository(),
            Substitute.For<ISocialPublishingUnitOfWork>(),
            Options.Create(new OrganicMediaOptions { MaxUploadBytes = 110_000_000 }));
    }

    private sealed class FixedMetadataReader(int width, int height, int? durationMs) : IOrganicMediaMetadataReader
    {
        public Task<BuildingBlocks.Domain.Primitives.Result<OrganicMediaMetadata>> ReadAsync(
            Stream content,
            string mimeType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BuildingBlocks.Domain.Primitives.Result<OrganicMediaMetadata>.Success(
                new OrganicMediaMetadata(width, height, durationMs)));
    }

    private sealed class InMemoryOrganicMediaAssetRepository : IOrganicMediaAssetRepository
    {
        public List<OrganicMediaAsset> Items { get; } = new();

        public Task<OrganicMediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(OrganicMediaAsset asset, CancellationToken cancellationToken = default)
        {
            Items.Add(asset);
            return Task.CompletedTask;
        }
    }
}