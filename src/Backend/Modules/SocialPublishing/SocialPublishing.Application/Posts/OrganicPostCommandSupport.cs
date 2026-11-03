using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Posts;

/// <summary>
/// Utilitários compartilhados pelos comandos de ciclo de vida de posts orgânicos.
/// </summary>
internal static class OrganicPostCommandSupport
{
    /// <summary>
    /// Carrega um post do workspace com tenant e usuário válidos.
    /// </summary>
    /// <param name="workspaceId">Workspace esperado.</param>
    /// <param name="postId">Identificador do post.</param>
    /// <param name="postRepository">Repositório de posts.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant.</param>
    /// <param name="currentUserContext">Contexto do usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Post carregado ou falha de negócio.</returns>
    public static async Task<Result<OrganicPost>> LoadPostAsync(
        Guid workspaceId,
        Guid postId,
        IOrganicPostRepository postRepository,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserContext currentUserContext,
        CancellationToken cancellationToken)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<OrganicPost>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (postId == Guid.Empty)
        {
            return Result<OrganicPost>.Failure(
                Error.Validation("Social.PostIdRequired", "O identificador do post é obrigatório."));
        }

        if (tenantContextAccessor.TenantContext is null)
        {
            return Result<OrganicPost>.Failure(SocialErrors.TenantRequired);
        }

        if (!currentUserContext.IsAuthenticated || currentUserContext.UserId is null || currentUserContext.UserId == Guid.Empty)
        {
            return Result<OrganicPost>.Failure(SocialErrors.UserRequired);
        }

        var post = await postRepository.GetByIdAsync(postId, cancellationToken);
        if (post is null || post.WorkspaceId != workspaceId)
        {
            return Result<OrganicPost>.Failure(SocialErrors.PostNotFound);
        }

        return Result<OrganicPost>.Success(post);
    }
}
