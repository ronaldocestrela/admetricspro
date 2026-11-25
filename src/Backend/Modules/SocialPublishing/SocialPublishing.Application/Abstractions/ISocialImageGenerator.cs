using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Planning.Images;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Porta para geração de imagens de feed via modelo Nano Banana (Gemini).
/// </summary>
public interface ISocialImageGenerator
{
    /// <summary>
    /// Gera bytes de imagem a partir do brief editorial.
    /// </summary>
    /// <param name="brief">Brief determinístico montado no servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Conteúdo binário e MIME, ou falha de transporte/provedor.</returns>
    Task<Result<GeneratedSocialImage>> GenerateAsync(ImageBrief brief, CancellationToken cancellationToken = default);
}

/// <summary>
/// Imagem gerada pelo provedor externo.
/// </summary>
/// <param name="Content">Bytes do arquivo de imagem.</param>
/// <param name="MimeType">Tipo MIME informado pelo adaptador.</param>
public sealed record GeneratedSocialImage(byte[] Content, string MimeType);