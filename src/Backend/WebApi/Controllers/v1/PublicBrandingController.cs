using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Branding;
using Tenants.Application.Branding.Queries.GetPublicTenantBrandAsset;

namespace WebApi.Controllers.v1;

/// <summary>
/// Entrega anônima de logos e favicon hospedados no object storage.
/// </summary>
[ApiController]
[Route("api/v1/public/branding")]
[AllowAnonymous]
public sealed class PublicBrandingController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa o controller de branding público.
    /// </summary>
    /// <param name="sender">Mediador in-memory.</param>
    public PublicBrandingController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Faz stream do ativo visual quando o tenant e o slot são válidos.
    /// </summary>
    /// <param name="tenantId">Identificador do inquilino (formato N).</param>
    /// <param name="slot">Slot (<c>light</c>, <c>dark</c>, <c>favicon</c>).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Stream binário ou erro opaco.</returns>
    [HttpGet("{tenantId}/{slot}")]
    [EndpointSummary("Leitura pública de logo ou favicon white-label")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetBrandAsset(
        [FromRoute] Guid tenantId,
        [FromRoute] string slot,
        CancellationToken cancellationToken = default)
    {
        if (!TenantBrandAssetSlotParser.TryParse(slot, out var parsedSlot))
        {
            return UnprocessableEntity(Result.Failure(TenantBrandAssetSlotParser.InvalidSlot));
        }

        var result = await _sender.Send(new GetPublicTenantBrandAssetQuery(tenantId, parsedSlot), cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Code == StorageErrors.KeyInvalid.Code)
            {
                return UnprocessableEntity(
                    Result.Failure(Error.Validation(result.Error.Code, result.Error.Description)));
            }

            return NotFound(Result.Failure(Error.NotFound("TenantBranding.AssetNotFound", "Ativo de marca não encontrado.")));
        }

        return File(result.Value.Content, result.Value.ContentType);
    }
}