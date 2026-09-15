using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Branding.Commands.UpdateTenantBranding;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Queries.GetTenantBranding;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo gerenciamento de identidade visual e personalização White-Label da organização no banco dedicado do inquilino.
/// </summary>
[ApiController]
[Route("api/v1/tenants/branding")]
public sealed class TenantBrandingController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantBrandingController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory de comandos e consultas.</param>
    public TenantBrandingController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Obtém os parâmetros completos de identidade visual e customização White-Label configurados para o inquilino ativo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados completos de branding ou fallback institucional.</returns>
    [HttpGet]
    [EndpointSummary("Obtém parâmetros completos de White-Label do inquilino autenticado")]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<TenantBrandingDetailsDto>>> GetBranding(
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTenantBrandingQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Atualiza as configurações de marca (cores, logomarcas claro/escuro e favicon) no banco de dados do inquilino.
    /// </summary>
    /// <param name="request">Dados de atualização contendo a paleta de cores e URLs de ativos visuais.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados de branding atualizados ou falha de validação.</returns>
    [HttpPut]
    [EndpointSummary("Atualiza a identidade visual White-Label do inquilino autenticado")]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<TenantBrandingDetailsDto>>> UpdateBranding(
        [FromBody] UpdateTenantBrandingApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<TenantBrandingDetailsDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new UpdateTenantBrandingCommand(
            request.PrimaryColor,
            request.SecondaryColor,
            request.LightLogoUrl,
            request.DarkLogoUrl,
            request.FaviconUrl);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Validation => UnprocessableEntity(result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}
