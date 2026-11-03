using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes de registro de mídia orgânica standalone (Subfase 2.4).
/// </summary>
public sealed class OrganicMediaAssetTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private const string ObjectKey = "social/tenant/ws/asset-1";

    /// <summary>
    /// FeedImage aceita proporção 4:5 (<see cref="AspectRatioRules.Label45"/>).
    /// </summary>
    [Fact]
    public void Register_FeedImageProporcao45_DeveTerSucesso()
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            OrganicPostFormat.FeedImage,
            OrganicMediaMimeTypes.Jpeg,
            durationMilliseconds: null,
            width: 1080,
            height: 1350,
            ObjectKey);

        result.IsSuccess.Should().BeTrue();
        result.Value.AspectLabel.Should().Be(AspectRatioRules.Label45);
        result.Value.ObjectKey.Should().Be(ObjectKey);
    }

    /// <summary>
    /// FeedImage aceita proporção 1:1 (<see cref="AspectRatioRules.Label11"/>).
    /// </summary>
    [Fact]
    public void Register_FeedImageProporcao11_DeveTerSucesso()
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            OrganicPostFormat.FeedImage,
            OrganicMediaMimeTypes.Png,
            null,
            1080,
            1080,
            ObjectKey);

        result.IsSuccess.Should().BeTrue();
        result.Value.AspectLabel.Should().Be(AspectRatioRules.Label11);
    }

    /// <summary>
    /// FeedImage rejeita proporção fora de 1:1 e 4:5.
    /// </summary>
    [Fact]
    public void Register_FeedImageProporcaoInvalida_DeveRetornarMediaAspectInvalid()
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            OrganicPostFormat.FeedImage,
            OrganicMediaMimeTypes.Jpeg,
            null,
            1920,
            1080,
            ObjectKey);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaAspectInvalid.Code);
    }

    /// <summary>
    /// Carrossel rejeita item com proporção diferente da âncora.
    /// </summary>
    [Fact]
    public void Register_CarrosselComProporcaoDiferenteDaAncora_DeveRetornarMediaAspectInvalid()
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            OrganicPostFormat.FeedCarousel,
            OrganicMediaMimeTypes.Jpeg,
            null,
            1080,
            1350,
            ObjectKey,
            carouselAnchorAspectLabel: AspectRatioRules.Label11);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaAspectInvalid.Code);
    }

    /// <summary>
    /// Reel exige 9:16 (<see cref="AspectRatioRules.Label916"/>).
    /// </summary>
    [Theory]
    [InlineData(OrganicPostFormat.Reel)]
    [InlineData(OrganicPostFormat.TikTokVideo)]
    public void Register_VideoVertical916_DeveTerSucesso(OrganicPostFormat format)
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            format,
            OrganicMediaMimeTypes.Mp4,
            durationMilliseconds: 15_000,
            width: 1080,
            height: 1920,
            ObjectKey);

        result.IsSuccess.Should().BeTrue();
        result.Value.AspectLabel.Should().Be(AspectRatioRules.Label916);
    }

    /// <summary>
    /// Reel e TikTok rejeitam proporção que não é 9:16.
    /// </summary>
    [Theory]
    [InlineData(OrganicPostFormat.Reel)]
    [InlineData(OrganicPostFormat.TikTokVideo)]
    public void Register_VideoForaDe916_DeveRetornarMediaAspectInvalid(OrganicPostFormat format)
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            format,
            OrganicMediaMimeTypes.Mp4,
            10_000,
            1920,
            1080,
            ObjectKey);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaAspectInvalid.Code);
    }

    /// <summary>
    /// MIME fora da lista permitida é rejeitado.
    /// </summary>
    [Fact]
    public void Register_MimeNaoPermitido_DeveRetornarMediaMimeInvalid()
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            OrganicPostFormat.FeedImage,
            "image/webp",
            null,
            1080,
            1080,
            ObjectKey);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaMimeInvalid.Code);
    }

    /// <summary>
    /// Vídeo em formato de feed é rejeitado.
    /// </summary>
    [Fact]
    public void Register_FeedImageComVideo_DeveRetornarMediaMimeInvalid()
    {
        var result = OrganicMediaAsset.Register(
            WorkspaceId,
            OrganicPostFormat.FeedImage,
            OrganicMediaMimeTypes.Mp4,
            5000,
            1080,
            1920,
            ObjectKey);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaMimeInvalid.Code);
    }
}
