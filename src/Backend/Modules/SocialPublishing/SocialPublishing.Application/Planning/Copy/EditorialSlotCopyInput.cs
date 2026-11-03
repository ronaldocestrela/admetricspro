using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Copy;

/// <summary>
/// Dados mínimos de um slot editorial para montagem de brief de cópia.
/// </summary>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="Platform">Rede de publicação.</param>
/// <param name="Format">Formato sugerido.</param>
public sealed record EditorialSlotCopyInput(
    ContentPillar Pillar,
    OrganicPlatform Platform,
    OrganicPostFormat Format);
