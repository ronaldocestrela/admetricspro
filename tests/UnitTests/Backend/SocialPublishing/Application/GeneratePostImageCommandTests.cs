using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Planning.Images;
using SocialPublishing.Application.Posts.Commands.GeneratePostImage;
using SocialPublishing.Domain;
using SocialPublishing.Infrastructure.Media;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="GeneratePostImageCommand"/>.
/// </summary>
public sealed class GeneratePostImageCommandTests
{
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>
    /// Valida permissão exigida no comando.
    /// </summary>
    [Fact]
    public void Command_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<GeneratePostImageCommand>();
    }

    /// <summary>
    /// Post aprovado deve anexar mídia e devolver DTO com item.
    /// </summary>
    [Fact]
    public async Task Handle_PostAprovado_DeveAnexarMidiaECommitar()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);

        var brandVoice = SeedBrandVoice(host.WorkspaceId);
        var imageGenerator = Substitute.For<ISocialImageGenerator>();
        imageGenerator.GenerateAsync(Arg.Any<ImageBrief>(), Arg.Any<CancellationToken>())
            .Returns(Result<GeneratedSocialImage>.Success(
                new GeneratedSocialImage(MinimalPng, OrganicMediaMimeTypes.Png)));

        var mediaStore = new InMemoryOrganicMediaStore();
        var handler = CreateHandler(host, brandVoice, imageGenerator, mediaStore);

        var result = await handler.Handle(
            new GeneratePostImageCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Media.Should().ContainSingle();
        post.Media.Should().ContainSingle();
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await imageGenerator.Received(1).GenerateAsync(Arg.Any<ImageBrief>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Segunda geração substitui a mídia anterior e remove a chave antiga do store.
    /// </summary>
    [Fact]
    public async Task Handle_SegundaGeracao_RemoveChaveAntiga()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);

        var brandVoice = SeedBrandVoice(host.WorkspaceId);
        var imageGenerator = Substitute.For<ISocialImageGenerator>();
        imageGenerator.GenerateAsync(Arg.Any<ImageBrief>(), Arg.Any<CancellationToken>())
            .Returns(Result<GeneratedSocialImage>.Success(
                new GeneratedSocialImage(MinimalPng, OrganicMediaMimeTypes.Png)));

        var mediaStore = new TrackingOrganicMediaStore();
        var handler = CreateHandler(host, brandVoice, imageGenerator, mediaStore);

        await handler.Handle(new GeneratePostImageCommand(host.WorkspaceId, post.Id), CancellationToken.None);
        var firstKey = mediaStore.LastDurableKey;
        firstKey.Should().NotBeNullOrWhiteSpace();

        await handler.Handle(new GeneratePostImageCommand(host.WorkspaceId, post.Id), CancellationToken.None);

        post.Media.Should().ContainSingle();
        mediaStore.DeletedKeys.Should().Contain(firstKey);
        mediaStore.Exists(firstKey!).Should().BeFalse();
        mediaStore.LastDurableKey.Should().NotBe(firstKey);
    }

    /// <summary>
    /// Post em Draft não deve chamar o gerador de imagem.
    /// </summary>
    [Fact]
    public async Task Handle_EmDraft_NaoDeveChamarGerador()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        var imageGenerator = Substitute.For<ISocialImageGenerator>();
        var handler = CreateHandler(host, SeedBrandVoice(host.WorkspaceId), imageGenerator, new InMemoryOrganicMediaStore());

        var result = await handler.Handle(
            new GeneratePostImageCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        await imageGenerator.DidNotReceive().GenerateAsync(Arg.Any<ImageBrief>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha do provedor não deve commitar.
    /// </summary>
    [Fact]
    public async Task Handle_FalhaDoModelo_NaoDeveCommitar()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);

        var imageGenerator = Substitute.For<ISocialImageGenerator>();
        imageGenerator.GenerateAsync(Arg.Any<ImageBrief>(), Arg.Any<CancellationToken>())
            .Returns(Result<GeneratedSocialImage>.Failure(SocialErrors.ImageGenerationFailed));

        var handler = CreateHandler(host, SeedBrandVoice(host.WorkspaceId), imageGenerator, new InMemoryOrganicMediaStore());

        var result = await handler.Handle(
            new GeneratePostImageCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static GeneratePostImageCommandHandler CreateHandler(
        OrganicPostLifecycleCommandTestHost host,
        BrandVoiceProfile brandVoice,
        ISocialImageGenerator imageGenerator,
        IOrganicMediaStore mediaStore)
    {
        var brandVoiceRepository = Substitute.For<IBrandVoiceRepository>();
        brandVoiceRepository.GetByWorkspaceAsync(host.WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(brandVoice);

        var mediaUrlResolver = Substitute.For<IOrganicMediaPublicUrlResolver>();
        mediaUrlResolver.ResolvePublicUrl(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<string>.Failure(SocialErrors.MediaPublicBaseUrlMissing));

        return new GeneratePostImageCommandHandler(
            host.PostRepository,
            brandVoiceRepository,
            imageGenerator,
            new ImageSharpFeedImageNormalizer(),
            mediaStore,
            mediaUrlResolver,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            Options.Create(new SocialImageAiOptions { ImageSize = "1K" }));
    }

    private static BrandVoiceProfile SeedBrandVoice(Guid workspaceId)
    {
        return BrandVoiceProfile.Create(
            workspaceId,
            "Tom",
            "Persona",
            [],
            [],
            EmojiPolicy.Sparingly,
            hashtagLimit: 20,
            authorityWeight: 40,
            saleWeight: 20,
            connectionWeight: 25,
            socialProofWeight: 15).Value;
    }

    private sealed class InMemoryOrganicMediaStore : IOrganicMediaStore
    {
        private readonly Dictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

        /// <inheritdoc />
        public Task<Result<string>> SaveAsync(
            Guid tenantId,
            Guid workspaceId,
            Guid assetId,
            Stream content,
            string contentType,
            OrganicMediaStorageTier tier,
            CancellationToken cancellationToken = default)
        {
            using var memory = new MemoryStream();
            content.CopyTo(memory);
            var key = $"organic-media/durable/{tenantId:N}/{workspaceId:N}/{assetId:N}";
            _objects[key] = memory.ToArray();
            return Task.FromResult(Result<string>.Success(key));
        }

        /// <inheritdoc />
        public Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(objectKey, out var bytes))
            {
                return Task.FromResult(Result<Stream>.Failure(
                    Error.Failure("Social.MediaNotFound", "Objeto não encontrado.")));
            }

            return Task.FromResult(Result<Stream>.Success(new MemoryStream(bytes)));
        }

        /// <inheritdoc />
        public Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            _objects.Remove(objectKey);
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class TrackingOrganicMediaStore : IOrganicMediaStore
    {
        private readonly Dictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

        public string? LastDurableKey { get; private set; }

        public List<string> DeletedKeys { get; } = new();

        public bool Exists(string key) => _objects.ContainsKey(key);

        public Task<Result<string>> SaveAsync(
            Guid tenantId,
            Guid workspaceId,
            Guid assetId,
            Stream content,
            string contentType,
            OrganicMediaStorageTier tier,
            CancellationToken cancellationToken = default)
        {
            using var memory = new MemoryStream();
            content.CopyTo(memory);
            var key = $"organic-media/durable/{tenantId:N}/{workspaceId:N}/{assetId:N}";
            _objects[key] = memory.ToArray();
            if (tier == OrganicMediaStorageTier.Durable)
            {
                LastDurableKey = key;
            }

            return Task.FromResult(Result<string>.Success(key));
        }

        public Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(objectKey, out var bytes))
            {
                return Task.FromResult(Result<Stream>.Failure(
                    Error.Failure("Social.MediaNotFound", "Objeto não encontrado.")));
            }

            return Task.FromResult(Result<Stream>.Success(new MemoryStream(bytes)));
        }

        public Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            DeletedKeys.Add(objectKey);
            _objects.Remove(objectKey);
            return Task.FromResult(Result.Success());
        }
    }
}