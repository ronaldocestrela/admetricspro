using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.DTOs;

/// <summary>
/// Resultado da materialização de um plano editorial (sucesso parcial explícito).
/// </summary>
/// <param name="CreatedPostIds">Identificadores dos posts orgânicos criados em Draft.</param>
/// <param name="FailedSlots">Slots que não viraram post, com código de erro.</param>
public sealed record GeneratedPlanDto(
    IReadOnlyList<Guid> CreatedPostIds,
    IReadOnlyList<FailedEditorialSlotDto> FailedSlots);

/// <summary>
/// Slot editorial que falhou na materialização.
/// </summary>
/// <param name="LocalDate">Data civil do slot.</param>
/// <param name="UtcInstant">Instante UTC do slot.</param>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="Platform">Rede sugerida.</param>
/// <param name="Format">Formato sugerido.</param>
/// <param name="ErrorCode">Código estável do erro.</param>
/// <param name="ErrorDescription">Descrição legível.</param>
public sealed record FailedEditorialSlotDto(
    DateOnly LocalDate,
    DateTime UtcInstant,
    ContentPillar Pillar,
    OrganicPlatform Platform,
    OrganicPostFormat Format,
    string ErrorCode,
    string ErrorDescription);
