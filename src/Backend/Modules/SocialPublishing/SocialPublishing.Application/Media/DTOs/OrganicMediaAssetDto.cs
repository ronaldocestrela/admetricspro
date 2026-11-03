using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Media.DTOs;

/// <summary>
/// Representação HTTP de um ativo de mídia orgânica registrado (sem binário).
/// </summary>
/// <param name="AssetId">Identificador do ativo.</param>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="Format">Formato de publicação alvo.</param>
/// <param name="MimeType">Tipo MIME.</param>
/// <param name="Width">Largura em pixels.</param>
/// <param name="Height">Altura em pixels.</param>
/// <param name="DurationMilliseconds">Duração do vídeo em ms, se aplicável.</param>
/// <param name="AspectLabel">Rótulo de proporção classificado.</param>
/// <param name="ObjectKey">Chave do objeto no armazenamento externo.</param>
public sealed record OrganicMediaAssetDto(
    Guid AssetId,
    Guid WorkspaceId,
    OrganicPostFormat Format,
    string MimeType,
    int Width,
    int Height,
    int? DurationMilliseconds,
    string AspectLabel,
    string ObjectKey);
