using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Approvals.DTOs;

/// <summary>
/// Projeção de post orgânico para o quadro público de aprovação (sem segredos).
/// </summary>
/// <param name="Id">Identificador do post.</param>
/// <param name="Caption">Legenda.</param>
/// <param name="Platform">Rede de destino.</param>
/// <param name="Format">Formato do post.</param>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="ScheduledAtUtc">Horário agendado em UTC.</param>
public sealed record PublicApprovalPostDto(
    Guid Id,
    string Caption,
    OrganicPlatform Platform,
    OrganicPostFormat Format,
    ContentPillar Pillar,
    DateTime ScheduledAtUtc);
