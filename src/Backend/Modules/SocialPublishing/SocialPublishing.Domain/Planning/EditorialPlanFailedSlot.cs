using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Domain.Planning;

/// <summary>
/// Slot editorial cujo rascunho não pôde ser criado durante a materialização do plano.
/// </summary>
/// <param name="LocalDate">Data civil do slot no fuso do workspace.</param>
/// <param name="UtcInstant">Instante UTC do slot.</param>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="Platform">Rede sugerida.</param>
/// <param name="Format">Formato sugerido.</param>
/// <param name="ErrorCode">Código estável do erro (<see cref="SocialErrors"/>).</param>
/// <param name="ErrorDescription">Descrição legível do erro.</param>
public sealed record EditorialPlanFailedSlot(
    DateOnly LocalDate,
    DateTime UtcInstant,
    ContentPillar Pillar,
    OrganicPlatform Platform,
    OrganicPostFormat Format,
    string ErrorCode,
    string ErrorDescription);
