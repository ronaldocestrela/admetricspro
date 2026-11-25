namespace SocialPublishing.Application.Posts.DTOs;

/// <summary>
/// Item de mídia anexado a um post orgânico para leitura na API.
/// </summary>
/// <param name="Id">Identificador do ativo.</param>
/// <param name="MimeType">Tipo MIME.</param>
/// <param name="Width">Largura em pixels.</param>
/// <param name="Height">Altura em pixels.</param>
/// <param name="AspectLabel">Rótulo de proporção classificado.</param>
/// <param name="PublicUrl">URL pública HTTPS quando configurada; ausente quando indisponível.</param>
public sealed record OrganicPostMediaItemDto(
    Guid Id,
    string MimeType,
    int Width,
    int Height,
    string? AspectLabel,
    string? PublicUrl);