using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using UnitTests.Frontend.Common;
using WebApp.Components.Social;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Services.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes bUnit do diálogo <see cref="OrganicPostDetailDialog"/>.
/// </summary>
public sealed class OrganicPostDetailDialogTests : BunitTestBase
{
    private readonly ISocialPublishingClient _socialClient = Substitute.For<ISocialPublishingClient>();

    /// <summary>
    /// Registra mock do cliente social.
    /// </summary>
    public OrganicPostDetailDialogTests()
    {
        Services.AddMemoryCache();
        Services.AddSingleton<ISocialMediaPreviewCache, SocialMediaPreviewCache>();
        Services.AddSingleton(_socialClient);
    }

    /// <summary>
    /// Post aprovado em feed deve exibir botão de gerar imagem.
    /// </summary>
    [Fact]
    public void ApprovedFeedImage_ShouldRenderGenerateImageButton()
    {
        var workspaceId = Guid.NewGuid();
        var post = SamplePost(workspaceId, OrganicPostStatus.Approved);

        var cut = Render<OrganicPostDetailDialog>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.Post, post));

        cut.Find("[data-testid='organic-post-detail-generate-image']").Should().NotBeNull();
    }

    /// <summary>
    /// Post em rascunho não deve exibir botão de gerar imagem.
    /// </summary>
    [Fact]
    public void Draft_ShouldNotRenderGenerateImageButton()
    {
        var workspaceId = Guid.NewGuid();
        var post = SamplePost(workspaceId, OrganicPostStatus.Draft);

        var cut = Render<OrganicPostDetailDialog>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.Post, post));

        cut.FindAll("[data-testid='organic-post-detail-generate-image']").Should().BeEmpty();
    }

    /// <summary>
    /// Preview deve usar a URL pública retornada pela API.
    /// </summary>
    [Fact]
    public void WithMedia_ShouldRenderPreviewUrl()
    {
        var workspaceId = Guid.NewGuid();
        var post = SamplePost(workspaceId, OrganicPostStatus.Approved) with
        {
            Media =
            [
                new OrganicPostMediaItemDto(
                    Guid.NewGuid(),
                    "image/png",
                    1080,
                    1350,
                    "4:5",
                    "https://cdn.test/preview.png"),
            ],
        };

        var cut = Render<OrganicPostDetailDialog>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.Post, post));

        var img = cut.Find("[data-testid='organic-post-detail-image-preview']");
        img.GetAttribute("src").Should().Be("https://cdn.test/preview.png");
    }

    /// <summary>
    /// Sem URL pública, a prévia usa o conteúdo autenticado da API.
    /// </summary>
    [Fact]
    public void WithoutPublicUrl_ShouldRenderTicketPreviewUrlFromApi()
    {
        var workspaceId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var post = SamplePost(workspaceId, OrganicPostStatus.Approved) with
        {
            Media =
            [
                new OrganicPostMediaItemDto(assetId, "image/png", 1080, 1350, "4:5", null),
            ],
        };

        const string previewBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        _socialClient.GetPostMediaContentAsync(workspaceId, post.Id, assetId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OrganicPostMediaContentDto>.Success(
                new OrganicPostMediaContentDto("image/png", previewBase64))));

        var cut = Render<OrganicPostDetailDialog>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.Post, post));

        cut.WaitForAssertion(() =>
        {
            var img = cut.Find("[data-testid='organic-post-detail-image-preview']");
            img.GetAttribute("src").Should().StartWith("/social/media-preview/");
        });
    }

    private static OrganicPostDto SamplePost(Guid workspaceId, OrganicPostStatus status) =>
        new(
            Guid.NewGuid(),
            workspaceId,
            Guid.Empty,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Legenda",
            new DateTime(2026, 11, 10, 13, 0, 0, DateTimeKind.Utc),
            status);
}