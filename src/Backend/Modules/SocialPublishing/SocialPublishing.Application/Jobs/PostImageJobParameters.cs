namespace SocialPublishing.Application.Jobs;

/// <summary>
/// Parâmetros mínimos de geração de imagem de post.
/// </summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="PostId">Post aprovado.</param>
/// <param name="Direction">Direcionamento visual opcional.</param>
public sealed record PostImageJobParameters(
    Guid WorkspaceId,
    Guid PostId,
    string? Direction);