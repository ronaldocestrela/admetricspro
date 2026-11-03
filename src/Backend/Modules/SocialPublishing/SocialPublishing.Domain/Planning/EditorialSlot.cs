using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Domain.Planning;

/// <summary>
/// Slot editorial planejado (data, rede e formato) ainda sem legenda ou mídia.
/// </summary>
/// <param name="LocalDate">Data civil no fuso do workspace.</param>
/// <param name="UtcInstant">Instante UTC do horário local do slot.</param>
/// <param name="Pillar">Pilar editorial atribuído.</param>
/// <param name="Platform">Rede de publicação sugerida.</param>
/// <param name="Format">Formato de publicação sugerido.</param>
public sealed record EditorialSlot(
    DateOnly LocalDate,
    DateTime UtcInstant,
    ContentPillar Pillar,
    OrganicPlatform Platform,
    OrganicPostFormat Format);
