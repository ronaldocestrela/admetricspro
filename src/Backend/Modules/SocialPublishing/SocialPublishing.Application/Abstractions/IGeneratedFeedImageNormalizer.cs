using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Normaliza imagens geradas por IA para proporção e resolução de feed orgânico.
/// </summary>
public interface IGeneratedFeedImageNormalizer
{
    /// <summary>
    /// Recorta e redimensiona o binário para o rótulo de proporção alvo (ex.: 4:5 ou 1:1).
    /// </summary>
    /// <param name="content">Bytes da imagem de entrada.</param>
    /// <param name="mimeType">Tipo MIME da entrada.</param>
    /// <param name="targetAspectLabel">Rótulo alvo conforme <see cref="Domain.Posts.AspectRatioRules"/>.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>PNG normalizado com dimensões finais ou falha.</returns>
    Task<Result<NormalizedFeedImage>> NormalizeAsync(
        byte[] content,
        string mimeType,
        string targetAspectLabel,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Imagem de feed normalizada para anexação ao post.
/// </summary>
/// <param name="Content">Bytes PNG.</param>
/// <param name="MimeType">Sempre image/png após normalização.</param>
/// <param name="Width">Largura final em pixels.</param>
/// <param name="Height">Altura final em pixels.</param>
public sealed record NormalizedFeedImage(byte[] Content, string MimeType, int Width, int Height);