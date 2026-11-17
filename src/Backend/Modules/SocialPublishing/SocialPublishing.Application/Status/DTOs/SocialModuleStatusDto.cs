namespace SocialPublishing.Application.Status.DTOs;

/// <summary>
/// Resumo operacional do módulo Social Publishing para um workspace.
/// </summary>
/// <param name="Connections">Quantidade de conexões orgânicas ativas ou registradas no workspace.</param>
/// <param name="ScheduledPosts">Quantidade de posts com status <c>Scheduled</c>.</param>
public sealed record SocialModuleStatusDto(int Connections, int ScheduledPosts);
