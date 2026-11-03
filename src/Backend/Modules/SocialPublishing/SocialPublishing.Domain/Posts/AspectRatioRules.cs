using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Regras nomeadas de proporção de mídia orgânica (tolerância relativa de 1%).
/// </summary>
public static class AspectRatioRules
{
    /// <summary>
    /// Rótulo de proporção quadrada 1:1.
    /// </summary>
    public const string Label11 = "1:1";

    /// <summary>
    /// Rótulo de proporção retrato 4:5.
    /// </summary>
    public const string Label45 = "4:5";

    /// <summary>
    /// Rótulo de proporção vertical 9:16.
    /// </summary>
    public const string Label916 = "9:16";

    private const double Target11 = 1.0;
    private const double Target45 = 4.0 / 5.0;
    private const double Target916 = 9.0 / 16.0;
    private const double RelativeTolerance = 0.01;

    /// <summary>
    /// Classifica as dimensões em um rótulo de proporção conhecido.
    /// </summary>
    /// <param name="width">Largura em pixels.</param>
    /// <param name="height">Altura em pixels.</param>
    /// <returns>Rótulo ou falha quando nenhuma proporção suportada se aplica.</returns>
    public static Result<string> Classify(int width, int height)
    {
        var aspect = AspectRatio.From(width, height);
        if (aspect.IsFailure)
        {
            return Result<string>.Failure(aspect.Error);
        }

        var ratio = aspect.Value.Ratio;
        if (IsWithinTolerance(ratio, Target11))
        {
            return Result<string>.Success(Label11);
        }

        if (IsWithinTolerance(ratio, Target45))
        {
            return Result<string>.Success(Label45);
        }

        if (IsWithinTolerance(ratio, Target916))
        {
            return Result<string>.Success(Label916);
        }

        return Result<string>.Failure(SocialErrors.MediaAspectInvalid);
    }

    /// <summary>
    /// Valida se o rótulo de proporção é permitido para o formato informado.
    /// </summary>
    /// <param name="format">Formato de publicação.</param>
    /// <param name="aspectLabel">Rótulo classificado.</param>
    /// <returns>Sucesso ou <see cref="SocialErrors.MediaAspectInvalid"/>.</returns>
    public static Result ValidateForFormat(OrganicPostFormat format, string aspectLabel)
    {
        var allowed = format switch
        {
            OrganicPostFormat.FeedImage or OrganicPostFormat.FeedCarousel =>
                aspectLabel is Label11 or Label45,
            OrganicPostFormat.Reel or OrganicPostFormat.TikTokVideo =>
                aspectLabel == Label916,
            _ => false
        };

        return allowed
            ? Result.Success()
            : Result.Failure(SocialErrors.MediaAspectInvalid);
    }

    /// <summary>
    /// Valida consistência de proporção entre item de carrossel e a mídia âncora.
    /// </summary>
    /// <param name="carouselAnchorAspectLabel">Rótulo da primeira mídia do carrossel, se houver.</param>
    /// <param name="itemAspectLabel">Rótulo do item atual.</param>
    /// <returns>Sucesso ou falha quando as proporções divergem.</returns>
    public static Result ValidateCarouselItem(string? carouselAnchorAspectLabel, string itemAspectLabel)
    {
        if (string.IsNullOrWhiteSpace(carouselAnchorAspectLabel))
        {
            return Result.Success();
        }

        return string.Equals(carouselAnchorAspectLabel, itemAspectLabel, StringComparison.Ordinal)
            ? Result.Success()
            : Result.Failure(SocialErrors.MediaAspectInvalid);
    }

    /// <summary>
    /// Indica se a razão informada está dentro da tolerância relativa do alvo.
    /// </summary>
    /// <param name="ratio">Razão largura/altura.</param>
    /// <param name="target">Alvo nominal.</param>
    /// <returns><c>true</c> quando dentro da tolerância.</returns>
    internal static bool IsWithinTolerance(double ratio, double target) =>
        Math.Abs(ratio - target) <= RelativeTolerance * target;
}
