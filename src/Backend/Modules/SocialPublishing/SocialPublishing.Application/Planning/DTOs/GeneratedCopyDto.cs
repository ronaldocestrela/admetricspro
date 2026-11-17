using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Planning.DTOs;

/// <summary>
/// Cópia validada para um slot editorial.
/// </summary>
/// <param name="Caption">Legenda aceita.</param>
/// <param name="HashtagGroups">Hashtags agrupadas por função.</param>
/// <param name="Network">Rede de publicação.</param>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="Warnings">Avisos não bloqueantes (ex.: link obrigatório ausente).</param>
public sealed record GeneratedCopyDto(
    string Caption,
    GeneratedHashtagGroupsDto HashtagGroups,
    OrganicPlatform Network,
    ContentPillar Pillar,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Hashtags agrupadas em Brand, Niche e Reach.
/// </summary>
/// <param name="Brand">Hashtags de marca.</param>
/// <param name="Niche">Hashtags de nicho.</param>
/// <param name="Reach">Hashtags de alcance.</param>
public sealed record GeneratedHashtagGroupsDto(
    IReadOnlyList<string> Brand,
    IReadOnlyList<string> Niche,
    IReadOnlyList<string> Reach);
