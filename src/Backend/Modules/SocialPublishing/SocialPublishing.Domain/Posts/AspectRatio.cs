using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Value object que representa a proporção largura/altura de uma mídia.
/// </summary>
public sealed class AspectRatio : ValueObject
{
    private const double Vertical916Target = 9.0 / 16.0;
    private const double RelativeTolerance = 0.01;

    private AspectRatio(int width, int height, double ratio)
    {
        Width = width;
        Height = height;
        Ratio = ratio;
    }

    /// <summary>
    /// Obtém a largura em pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Obtém a altura em pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Obtém a razão largura/altura.
    /// </summary>
    public double Ratio { get; }

    /// <summary>
    /// Cria uma proporção a partir das dimensões informadas.
    /// </summary>
    /// <param name="width">Largura em pixels.</param>
    /// <param name="height">Altura em pixels.</param>
    /// <returns>Resultado com a proporção ou erro de validação.</returns>
    public static Result<AspectRatio> From(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return Result<AspectRatio>.Failure(
                Error.Validation("Social.MediaDimensionsInvalid", "Largura e altura devem ser positivas."));
        }

        return Result<AspectRatio>.Success(new AspectRatio(width, height, (double)width / height));
    }

    /// <summary>
    /// Indica se a proporção está dentro da tolerância de 9:16 (vertical).
    /// </summary>
    /// <returns><c>true</c> quando a proporção é vertical 9:16 dentro de 1% relativo.</returns>
    public bool IsVertical916()
    {
        var delta = Math.Abs(Ratio - Vertical916Target);
        return delta <= RelativeTolerance * Vertical916Target;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Width;
        yield return Height;
    }
}
