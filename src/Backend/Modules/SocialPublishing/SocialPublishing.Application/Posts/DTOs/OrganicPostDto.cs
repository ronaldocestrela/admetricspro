using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Posts.DTOs;

/// <summary>
/// Projeção de leitura de um post orgânico para calendário e listagens.
/// </summary>
/// <param name="Id">Identificador do post.</param>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="ConnectionId">Conexão orgânica associada.</param>
/// <param name="Platform">Rede de destino.</param>
/// <param name="Format">Formato do post.</param>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="Caption">Legenda persistida.</param>
/// <param name="ScheduledAtUtc">Horário agendado em UTC.</param>
/// <param name="Status">Status atual do post.</param>
public sealed record OrganicPostDto(
    Guid Id,
    Guid WorkspaceId,
    Guid ConnectionId,
    OrganicPlatform Platform,
    OrganicPostFormat Format,
    ContentPillar Pillar,
    string Caption,
    DateTime ScheduledAtUtc,
    OrganicPostStatus Status);
