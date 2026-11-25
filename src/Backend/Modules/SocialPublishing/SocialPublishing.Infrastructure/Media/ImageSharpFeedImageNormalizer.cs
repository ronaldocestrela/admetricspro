using BuildingBlocks.Domain.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Recorta e redimensiona imagens geradas para proporções de feed suportadas.
/// </summary>
public sealed class ImageSharpFeedImageNormalizer : IGeneratedFeedImageNormalizer
{
    private const int FeedWidth = 1080;
    private const int FeedHeight45 = 1350;
    private const int FeedHeight11 = 1080;

    /// <inheritdoc />
    public async Task<Result<NormalizedFeedImage>> NormalizeAsync(
        byte[] content,
        string mimeType,
        string targetAspectLabel,
        CancellationToken cancellationToken = default)
    {
        if (content is null || content.Length == 0)
        {
            return Result<NormalizedFeedImage>.Failure(SocialErrors.ImageGenerationFailed);
        }

        if (!TryResolveTargetSize(targetAspectLabel, out var targetWidth, out var targetHeight, out var targetRatio))
        {
            return Result<NormalizedFeedImage>.Failure(SocialErrors.MediaAspectInvalid);
        }

        try
        {
            using var input = new MemoryStream(content, writable: false);
            using var image = await Image.LoadAsync(input, cancellationToken);

            var crop = ComputeCenterCrop(image.Width, image.Height, targetRatio);
            image.Mutate(ctx =>
            {
                ctx.Crop(crop);
                ctx.Resize(targetWidth, targetHeight);
            });

            await using var output = new MemoryStream();
            await image.SaveAsPngAsync(output, cancellationToken);

            return Result<NormalizedFeedImage>.Success(
                new NormalizedFeedImage(
                    output.ToArray(),
                    OrganicMediaMimeTypes.Png,
                    targetWidth,
                    targetHeight));
        }
        catch (UnknownImageFormatException)
        {
            return Result<NormalizedFeedImage>.Failure(SocialErrors.ImageGenerationFailed);
        }
        catch (InvalidImageContentException)
        {
            return Result<NormalizedFeedImage>.Failure(SocialErrors.ImageGenerationFailed);
        }
    }

    private static bool TryResolveTargetSize(
        string targetAspectLabel,
        out int targetWidth,
        out int targetHeight,
        out double targetRatio)
    {
        targetWidth = FeedWidth;
        targetHeight = FeedHeight45;
        targetRatio = 4.0 / 5.0;

        if (string.Equals(targetAspectLabel, AspectRatioRules.Label45, StringComparison.Ordinal))
        {
            targetHeight = FeedHeight45;
            targetRatio = 4.0 / 5.0;
            return true;
        }

        if (string.Equals(targetAspectLabel, AspectRatioRules.Label11, StringComparison.Ordinal))
        {
            targetHeight = FeedHeight11;
            targetRatio = 1.0;
            return true;
        }

        return false;
    }

    private static Rectangle ComputeCenterCrop(int sourceWidth, int sourceHeight, double targetRatio)
    {
        var sourceRatio = (double)sourceWidth / sourceHeight;
        int cropWidth;
        int cropHeight;

        if (sourceRatio > targetRatio)
        {
            cropHeight = sourceHeight;
            cropWidth = Math.Max(1, (int)Math.Round(sourceHeight * targetRatio));
        }
        else
        {
            cropWidth = sourceWidth;
            cropHeight = Math.Max(1, (int)Math.Round(sourceWidth / targetRatio));
        }

        var x = Math.Max(0, (sourceWidth - cropWidth) / 2);
        var y = Math.Max(0, (sourceHeight - cropHeight) / 2);

        return new Rectangle(x, y, cropWidth, cropHeight);
    }
}