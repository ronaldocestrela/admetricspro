using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SocialPublishing.Application.Approvals;
using SocialPublishing.Application.Approvals.Commands.DecidePublicApproval;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Application.Approvals.Queries.GetPublicApprovalBoard;
using SocialPublishing.Domain;
using Tenants.Application.Auth.Queries.GetTenantPublicBranding;
using Tenants.Application.Branding.Queries.GetTenantBranding;

namespace WebApi.Controllers.v1;

/// <summary>
/// Endpoints anônimos do portal white-label de aprovação orgânica.
/// </summary>
[ApiController]
[Route("api/v1/public/social/approvals")]
[AllowAnonymous]
public sealed class PublicSocialApprovalsController : ControllerBase
{
    private const string DefaultPrimaryColor = "#2563EB";
    private const string DefaultAgencyName = "Agência";

    private readonly ISender _sender;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PublicSocialApprovalsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant resolvido pelo pipeline HTTP.</param>
    public PublicSocialApprovalsController(ISender sender, ITenantContextAccessor tenantContextAccessor)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <summary>
    /// Obtém o quadro de posts pendentes e branding da agência para um token de aprovação.
    /// </summary>
    /// <param name="token">Token opaco da URL.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>DTO composto com posts e identidade visual.</returns>
    [HttpGet("{token}")]
    [EndpointSummary("Consulta pública do quadro de aprovação orgânica por token")]
    [ProducesResponseType(typeof(Result<PublicApprovalBoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PublicApprovalBoardDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<PublicApprovalBoardDto>>> GetApprovalBoard(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        var boardResult = await _sender.Send(new GetPublicApprovalBoardQuery(token), cancellationToken);
        if (boardResult.IsFailure)
        {
            return NotFound(NormalizePublicFailure(boardResult.Error));
        }

        var brandingResult = await _sender.Send(new GetTenantBrandingQuery(), cancellationToken);
        if (brandingResult.IsFailure)
        {
            return NotFound(NormalizePublicFailure(SocialErrors.ApprovalTokenInvalid));
        }

        var agencyName = DefaultAgencyName;
        var subdomain = _tenantContextAccessor.TenantContext?.Subdomain;
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            var publicBranding = await _sender.Send(new GetTenantPublicBrandingQuery(subdomain), cancellationToken);
            if (publicBranding.IsSuccess && !string.IsNullOrWhiteSpace(publicBranding.Value.CompanyName))
            {
                agencyName = publicBranding.Value.CompanyName;
            }
        }

        var dto = new PublicApprovalBoardDto(
            boardResult.Value.Posts,
            brandingResult.Value.PrimaryColor ?? DefaultPrimaryColor,
            brandingResult.Value.LightLogoUrl,
            agencyName);

        return Ok(Result<PublicApprovalBoardDto>.Success(dto));
    }

    /// <summary>
    /// Registra decisão do cliente final sobre posts selecionados.
    /// </summary>
    /// <param name="token">Token opaco da URL.</param>
    /// <param name="request">Posts selecionados e decisão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost("{token}/decisions")]
    [EndpointSummary("Decisão pública de aprovação orgânica por token")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> Decide(
        [FromRoute] string token,
        [FromBody] PublicApprovalDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return NotFound(NormalizePublicFailure(SocialErrors.ApprovalTokenInvalid));
        }

        var command = new DecidePublicApprovalCommand(
            token,
            request.PostIds ?? Array.Empty<Guid>(),
            request.Decision,
            request.Comment);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure &&
            (result.Error.Code == SocialErrors.ApprovalTokenInvalid.Code ||
             result.Error.Code == SocialErrors.TenantRequired.Code))
        {
            return NotFound(NormalizePublicFailure(SocialErrors.ApprovalTokenInvalid));
        }

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    private static Result NormalizePublicFailure(Error _) =>
        Result.Failure(SocialErrors.ApprovalTokenInvalid);

    private static Result<T> NormalizePublicFailure<T>(Error _) =>
        Result<T>.Failure(SocialErrors.ApprovalTokenInvalid);
}

