using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Status.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Status.Queries.GetSocialModuleStatus;

/// <summary>
/// Manipulador da consulta <see cref="GetSocialModuleStatusQuery"/>.
/// </summary>
public sealed class GetSocialModuleStatusQueryHandler : IQueryHandler<GetSocialModuleStatusQuery, SocialModuleStatusDto>
{
    private readonly IOrganicConnectionRepository _connectionRepository;
    private readonly IOrganicPostRepository _postRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetSocialModuleStatusQueryHandler"/>.
    /// </summary>
    /// <param name="connectionRepository">Repositório de conexões orgânicas.</param>
    /// <param name="postRepository">Repositório de posts orgânicos.</param>
    public GetSocialModuleStatusQueryHandler(
        IOrganicConnectionRepository connectionRepository,
        IOrganicPostRepository postRepository)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
    }

    /// <inheritdoc />
    public async Task<Result<SocialModuleStatusDto>> Handle(
        GetSocialModuleStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<SocialModuleStatusDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        var connections = await _connectionRepository.CountByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var scheduledPosts = await _postRepository.CountByWorkspaceAndStatusAsync(
            request.WorkspaceId,
            OrganicPostStatus.Scheduled,
            cancellationToken);

        return Result<SocialModuleStatusDto>.Success(new SocialModuleStatusDto(connections, scheduledPosts));
    }
}
