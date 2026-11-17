using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Approvals;
using SocialPublishing.Application.Approvals.DTOs;

namespace WebApp.Services.Social;

/// <summary>
/// Cliente HTTP anônimo para o portal público de aprovação orgânica.
/// </summary>
public interface IPublicSocialApprovalClient
{
    /// <summary>
    /// Carrega o quadro de aprovação e branding para um token.
    /// </summary>
    /// <param name="token">Token opaco da URL.</param>
    /// <param name="tenantSlug">Subdomínio opcional (<c>/t/{slug}/approve</c>).</param>
    /// <param name="browserHost">Host do browser para resolução CNAME/subdomínio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<Result<PublicApprovalBoardDto>> GetBoardAsync(
        string token,
        string? tenantSlug,
        string? browserHost,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia decisão sobre posts selecionados.
    /// </summary>
    /// <param name="token">Token opaco da URL.</param>
    /// <param name="postIds">Posts marcados.</param>
    /// <param name="decision">Aprovar ou solicitar alterações.</param>
    /// <param name="comment">Comentário quando aplicável.</param>
    /// <param name="tenantSlug">Subdomínio opcional.</param>
    /// <param name="browserHost">Host do browser.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<Result> SubmitDecisionAsync(
        string token,
        IReadOnlyList<Guid> postIds,
        PublicApprovalDecision decision,
        string? comment,
        string? tenantSlug,
        string? browserHost,
        CancellationToken cancellationToken = default);
}
