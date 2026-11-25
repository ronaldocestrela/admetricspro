using SocialPublishing.Application.Abstractions;
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
    /// <param name="publicUrlResolver">Resolvedor opcional de URL pública de mídia.</param>
    /// <returns>DTO projetado.</returns>
    public static OrganicPostDto ToDto(OrganicPost post, IOrganicMediaPublicUrlResolver? publicUrlResolver = null) =>
        new(
            post.Id,
            post.WorkspaceId,
            post.ConnectionId,
            post.Platform,
            post.Format,
            post.Pillar,
            post.Caption,
            post.ScheduledAtUtc,
            post.Status,
            post.ClientApprovalComment,
            MapMedia(post, publicUrlResolver));

    private static IReadOnlyList<OrganicPostMediaItemDto> MapMedia(
        OrganicPost post,
        IOrganicMediaPublicUrlResolver? publicUrlResolver)
    {
        if (post.Media.Count == 0)
        {
            return Array.Empty<OrganicPostMediaItemDto>();
        }

        var items = new List<OrganicPostMediaItemDto>(post.Media.Count);
        foreach (var asset in post.Media.OrderBy(m => m.SortOrder))
        {
            string? publicUrl = null;
            if (publicUrlResolver is not null &&
                !string.IsNullOrWhiteSpace(asset.ObjectKey))
            {
                var urlResult = publicUrlResolver.ResolvePublicUrl(asset.ObjectKey);
                if (urlResult.IsSuccess)
                {
                    publicUrl = urlResult.Value;
                }
            }

            items.Add(new OrganicPostMediaItemDto(
                asset.Id,
                asset.MimeType ?? "application/octet-stream",
                asset.Width,
                asset.Height,
                asset.AspectLabel,
                publicUrl));
        }

        return items;
    }
}
