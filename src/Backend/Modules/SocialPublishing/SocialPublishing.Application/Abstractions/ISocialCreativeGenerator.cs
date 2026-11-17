using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Planning.Copy;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Porta para geração de legendas e hashtags via modelo de linguagem.
/// </summary>
public interface ISocialCreativeGenerator
{
    /// <summary>
    /// Gera rascunho de cópia a partir do brief editorial.
    /// </summary>
    /// <param name="brief">Brief determinístico montado a partir do Brand Voice e do slot.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Rascunho bruto ou falha de transporte/provedor.</returns>
    Task<Result<GeneratedCopyDraft>> GenerateAsync(CopyBrief brief, CancellationToken cancellationToken = default);
}
