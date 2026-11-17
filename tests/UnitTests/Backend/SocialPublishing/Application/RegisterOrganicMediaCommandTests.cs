using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media.Commands.RegisterOrganicMedia;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using Microsoft.Extensions.Options;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="RegisterOrganicMediaCommandHandler"/>.
/// </summary>
public sealed class RegisterOrganicMediaCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly FakeOrganicMediaStore _store = new();
    private readonly InMemoryOrganicMediaAssetRepository _repository = new();
    private readonly ISocialPublishingUnitOfWork _unitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();

    /// <summary>
    /// Persiste somente a chave devolvida pelo store, sem bytes no repositório.
    /// </summary>
    [Fact]
    public async Task Handle_ComMidiaValida_DevePersistirSomenteObjectKey()
    {
        var handler = CreateHandler(new FixedMetadataReader(1080, 1080, null));
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await handler.Handle(
            new RegisterOrganicMediaCommand(
                _workspaceId,
                OrganicPostFormat.FeedImage,
                OrganicMediaMimeTypes.Jpeg,
                stream),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Items.Should().ContainSingle();
        var asset = _repository.Items[0];
        asset.ObjectKey.Should().Be(_store.LastDurableKey);
        asset.ObjectKey.Should().NotBeNullOrWhiteSpace();
        asset.GetType().GetProperties().Should().NotContain(p => p.PropertyType == typeof(byte[]));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Proporção inválida remove staging e não chama repositório.
    /// </summary>
    [Fact]
    public async Task Handle_ComProporcaoInvalida_DeveApagarStagingENaoPersistir()
    {
        var handler = CreateHandler(new FixedMetadataReader(1920, 1080, null));
        await using var stream = new MemoryStream([1, 2, 3]);

        var result = await handler.Handle(
            new RegisterOrganicMediaCommand(
                _workspaceId,
                OrganicPostFormat.FeedImage,
                OrganicMediaMimeTypes.Jpeg,
                stream),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaAspectInvalid.Code);
        _repository.Items.Should().BeEmpty();
        _store.DeletedKeys.Should().NotBeEmpty();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Workspace vazio retorna erro de negócio.
    /// </summary>
    [Fact]
    public async Task Handle_ComWorkspaceVazio_DeveRetornarWorkspaceRequired()
    {
        var handler = CreateHandler(new FixedMetadataReader(1080, 1080, null));
        await using var stream = new MemoryStream([1]);

        var result = await handler.Handle(
            new RegisterOrganicMediaCommand(
                Guid.Empty,
                OrganicPostFormat.FeedImage,
                OrganicMediaMimeTypes.Jpeg,
                stream),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
    }

    private RegisterOrganicMediaCommandHandler CreateHandler(IOrganicMediaMetadataReader metadataReader)
    {
        var tenantAccessor = new TenantContextAccessor();
        tenantAccessor.TenantContext = TenantContext.Create(
            _tenantId,
            "slug",
            TenantResolutionSource.Header);

        return new RegisterOrganicMediaCommandHandler(
            tenantAccessor,
            _store,
            metadataReader,
            _repository,
            _unitOfWork,
            Options.Create(new OrganicMediaOptions()));
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

    private sealed class FakeOrganicMediaStore : IOrganicMediaStore
    {
        public string? LastDurableKey { get; private set; }

        public List<string> DeletedKeys { get; } = new();

        private readonly Dictionary<string, byte[]> _objects = new();

        public Task<BuildingBlocks.Domain.Primitives.Result<string>> SaveAsync(
            Guid tenantId,
            Guid workspaceId,
            Guid assetId,
            Stream content,
            string contentType,
            OrganicMediaStorageTier tier,
            CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var key = tier == OrganicMediaStorageTier.Staging
                ? $"staging/{assetId:N}"
                : $"durable/{tenantId:N}/{workspaceId:N}/{assetId:N}";
            _objects[key] = ms.ToArray();
            if (tier == OrganicMediaStorageTier.Durable)
            {
                LastDurableKey = key;
            }

            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<string>.Success(key));
        }

        public Task<BuildingBlocks.Domain.Primitives.Result<Stream>> OpenReadAsync(
            string objectKey,
            CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(objectKey, out var bytes))
            {
                return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<Stream>.Failure(
                    BuildingBlocks.Domain.Primitives.Error.NotFound("test", "missing")));
            }

            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<Stream>.Success(
                new MemoryStream(bytes, writable: false)));
        }

        public Task<BuildingBlocks.Domain.Primitives.Result> DeleteAsync(
            string objectKey,
            CancellationToken cancellationToken = default)
        {
            DeletedKeys.Add(objectKey);
            _objects.Remove(objectKey);
            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result.Success());
        }
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
