using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Lê dimensões e duração a partir do cabeçalho de arquivos de mídia orgânica.
/// </summary>
public interface IOrganicMediaMetadataReader
{
    /// <summary>
    /// Extrai metadados do fluxo informado.
    /// </summary>
    /// <param name="content">Conteúdo do arquivo.</param>
    /// <param name="mimeType">Tipo MIME declarado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados ou <c>Social.MediaUnreadable</c>.</returns>
    Task<Result<OrganicMediaMetadata>> ReadAsync(Stream content, string mimeType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadados extraídos de um arquivo de mídia orgânica.
/// </summary>
/// <param name="Width">Largura em pixels.</param>
/// <param name="Height">Altura em pixels.</param>
/// <param name="DurationMilliseconds">Duração em ms para vídeo; nulo para imagem.</param>
public sealed record OrganicMediaMetadata(int Width, int Height, int? DurationMilliseconds);
