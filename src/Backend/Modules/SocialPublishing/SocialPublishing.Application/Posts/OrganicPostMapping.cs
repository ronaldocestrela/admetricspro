using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Posts;

/// <summary>
/// Mapeamento de agregados de post orgânico para DTOs de leitura.
/// </summary>
public static class OrganicPostMapping
{
    /// <summary>
    /// Converte um <see cref="OrganicPost"/> em DTO de leitura.
    /// </summary>
    /// <param name="post">Agregado de origem.</param>
    /// <returns>DTO projetado.</returns>
    public static OrganicPostDto ToDto(OrganicPost post) =>
        new(
            post.Id,
            post.WorkspaceId,
            post.ConnectionId,
            post.Platform,
            post.Format,
            post.Pillar,
            post.Caption,
            post.ScheduledAtUtc,
            post.Status);
}
