using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Posts.Queries.ListOrganicPosts;

/// <summary>
/// Handler da consulta de posts orgânicos por intervalo de agendamento.
/// </summary>
public sealed class ListOrganicPostsQueryHandler : IQueryHandler<ListOrganicPostsQuery, IReadOnlyList<OrganicPostDto>>
{
    private readonly IOrganicPostRepository _postRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ListOrganicPostsQueryHandler"/>.
    /// </summary>
    /// <param name="postRepository">Repositório de posts orgânicos.</param>
    public ListOrganicPostsQueryHandler(IOrganicPostRepository postRepository)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrganicPostDto>>> Handle(
        ListOrganicPostsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<OrganicPostDto>>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (request.ToUtc < request.FromUtc)
        {
            return Result<IReadOnlyList<OrganicPostDto>>.Failure(SocialErrors.PostsRangeInvalid);
        }

        var fromUtc = DateTime.SpecifyKind(request.FromUtc, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(request.ToUtc, DateTimeKind.Utc);

        var posts = await _postRepository.ListByWorkspaceScheduledRangeAsync(
            request.WorkspaceId,
            fromUtc,
            toUtc,
            cancellationToken);

        var dtos = posts.Select(OrganicPostMapping.ToDto).ToList();
        return Result<IReadOnlyList<OrganicPostDto>>.Success(dtos);
    }
}
