using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Domain.Planning;

namespace SocialPublishing.Application.Planning;

/// <summary>
/// Mapeamento de agregados de plano editorial para DTOs de API.
/// </summary>
public static class EditorialPlanMapping
{
    /// <summary>
    /// Converte um snapshot concluído em DTO de resposta.
    /// </summary>
    /// <param name="generation">Registro idempotente do plano.</param>
    /// <returns>DTO com posts criados e falhas.</returns>
    public static GeneratedPlanDto ToDto(EditorialPlanGeneration generation) =>
        new(
            generation.CreatedPostIds.ToList(),
            generation.FailedSlots
                .Select(f => new FailedEditorialSlotDto(
                    f.LocalDate,
                    f.UtcInstant,
                    f.Pillar,
                    f.Platform,
                    f.Format,
                    f.ErrorCode,
                    f.ErrorDescription))
                .ToList());
}
