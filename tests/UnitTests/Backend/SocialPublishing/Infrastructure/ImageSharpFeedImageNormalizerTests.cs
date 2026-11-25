using FluentAssertions;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Infrastructure.Media;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes de normalização de imagem gerada para feed.
/// </summary>
public sealed class ImageSharpFeedImageNormalizerTests
{
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>
    /// Proporção 4:5 deve sair em 1080x1350.
    /// </summary>
    [Fact]
    public async Task NormalizeAsync_Para45_DeveProduzir1080x1350()
    {
        var normalizer = new ImageSharpFeedImageNormalizer();

        var result = await normalizer.NormalizeAsync(
            MinimalPng,
            OrganicMediaMimeTypes.Png,
            AspectRatioRules.Label45,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Width.Should().Be(1080);
        result.Value.Height.Should().Be(1350);
        result.Value.MimeType.Should().Be(OrganicMediaMimeTypes.Png);
        result.Value.Content.Length.Should().BeGreaterThan(100);
    }

    /// <summary>
    /// Proporção 1:1 deve sair em 1080x1080.
    /// </summary>
    [Fact]
    public async Task NormalizeAsync_Para11_DeveProduzir1080x1080()
    {
        var normalizer = new ImageSharpFeedImageNormalizer();

        var result = await normalizer.NormalizeAsync(
            MinimalPng,
            OrganicMediaMimeTypes.Png,
            AspectRatioRules.Label11,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Width.Should().Be(1080);
        result.Value.Height.Should().Be(1080);
    }
}