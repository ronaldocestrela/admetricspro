using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Branding;
using Tenants.Application.Branding.Commands.DeleteTenantBrandAsset;
using Tenants.Application.Branding.Commands.UpdateTenantBranding;
using Tenants.Application.Branding.Commands.UploadTenantBrandAsset;
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
        var result = await _sender.Send(new GetTenantBrandingQuery(ResolveRequestOrigin()), cancellationToken);
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
            request.FaviconUrl,
            ResolveRequestOrigin());

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

    /// <summary>
    /// Envia logo ou favicon para o bucket de branding do inquilino.
    /// </summary>
    /// <param name="slot">Slot (<c>light</c>, <c>dark</c>, <c>favicon</c>).</param>
    /// <param name="file">Arquivo de imagem.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Branding atualizado com URL da API.</returns>
    [HttpPost("assets/{slot}")]
    [RequestSizeLimit(TenantBrandingAssetLimits.MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = TenantBrandingAssetLimits.MaxUploadBytes)]
    [EndpointSummary("Envia logo ou favicon white-label para object storage")]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<TenantBrandingDetailsDto>>> UploadBrandAsset(
        [FromRoute] string slot,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (!TenantBrandAssetSlotParser.TryParse(slot, out var parsedSlot))
        {
            return UnprocessableEntity(Result<TenantBrandingDetailsDto>.Failure(TenantBrandAssetSlotParser.InvalidSlot));
        }

        if (file is null || file.Length < 1)
        {
            return BadRequest(Result<TenantBrandingDetailsDto>.Failure(
                Error.Validation("TenantBranding.AssetFileRequired", "O arquivo de marca é obrigatório.")));
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadTenantBrandAssetCommand(
            parsedSlot,
            file.FileName,
            file.ContentType ?? "application/octet-stream",
            stream,
            ResolveRequestOrigin());

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result),
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Remove o objeto hospedado no slot e zera URL e metadados.
    /// </summary>
    /// <param name="slot">Slot (<c>light</c>, <c>dark</c>, <c>favicon</c>).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Branding atualizado.</returns>
    [HttpDelete("assets/{slot}")]
    [EndpointSummary("Remove logo ou favicon hospedado no object storage")]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<TenantBrandingDetailsDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<TenantBrandingDetailsDto>>> DeleteBrandAsset(
        [FromRoute] string slot,
        CancellationToken cancellationToken = default)
    {
        if (!TenantBrandAssetSlotParser.TryParse(slot, out var parsedSlot))
        {
            return UnprocessableEntity(Result<TenantBrandingDetailsDto>.Failure(TenantBrandAssetSlotParser.InvalidSlot));
        }

        var result = await _sender.Send(
            new DeleteTenantBrandAssetCommand(parsedSlot, ResolveRequestOrigin()),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result),
            };
        }

        return Ok(result);
    }

    private string? ResolveRequestOrigin()
    {
        if (Request?.Host.HasValue != true)
        {
            return null;
        }

        if (Request.IsHttps)
        {
            return $"{Request.Scheme}://{Request.Host.Value}";
        }

        return null;
    }
}
