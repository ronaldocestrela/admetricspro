using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Posts.Queries.GetOrganicPostMediaContent;

/// <summary>
/// Abre o objeto armazenado da mídia do post e devolve o conteúdo em Base64.
/// </summary>
public sealed class GetOrganicPostMediaContentQueryHandler
    : IQueryHandler<GetOrganicPostMediaContentQuery, OrganicPostMediaContentDto>
{
    private readonly IOrganicPostRepository _postRepository;
    private readonly IOrganicMediaStore _mediaStore;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetOrganicPostMediaContentQueryHandler"/>.
    /// </summary>
    /// <param name="postRepository">Repositório de posts.</param>
    /// <param name="mediaStore">Armazenamento de binário.</param>
    public GetOrganicPostMediaContentQueryHandler(
        IOrganicPostRepository postRepository,
        IOrganicMediaStore mediaStore)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _mediaStore = mediaStore ?? throw new ArgumentNullException(nameof(mediaStore));
    }

    /// <inheritdoc />
    public async Task<Result<OrganicPostMediaContentDto>> Handle(
        GetOrganicPostMediaContentQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<OrganicPostMediaContentDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (request.PostId == Guid.Empty || request.AssetId == Guid.Empty)
        {
            return Result<OrganicPostMediaContentDto>.Failure(
                Error.Validation("Social.PostIdRequired", "O identificador do post e da mídia são obrigatórios."));
        }

        var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
        if (post is null || post.WorkspaceId != request.WorkspaceId)
        {
            return Result<OrganicPostMediaContentDto>.Failure(SocialErrors.PostNotFound);
        }

        var asset = post.Media.FirstOrDefault(m => m.Id == request.AssetId);
        if (asset is null || string.IsNullOrWhiteSpace(asset.ObjectKey))
        {
            return Result<OrganicPostMediaContentDto>.Failure(
                Error.NotFound("Social.MediaNotFound", "Mídia do post não encontrada."));
        }

        var open = await _mediaStore.OpenReadAsync(asset.ObjectKey, cancellationToken);
        if (open.IsFailure)
        {
            return Result<OrganicPostMediaContentDto>.Failure(open.Error);
        }

        await using (open.Value)
        {
            using var buffer = new MemoryStream();
            await open.Value.CopyToAsync(buffer, cancellationToken);
            var dto = new OrganicPostMediaContentDto(
                asset.MimeType ?? "application/octet-stream",
                Convert.ToBase64String(buffer.ToArray()));
            return Result<OrganicPostMediaContentDto>.Success(dto);
        }
    }
}
