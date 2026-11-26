using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SocialPublishing.Application.Media.Queries.GetPublicOrganicMediaStream;
using SocialPublishing.Domain;

namespace WebApi.Controllers.v1;

/// <summary>
/// Entrega anônima de mídia orgânica para redes sociais via token HMAC de vida curta.
/// </summary>
[ApiController]
[Route("api/v1/public/media")]
[AllowAnonymous]
public sealed class PublicMediaController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa o controller de mídia pública.
    /// </summary>
    /// <param name="sender">Mediador in-memory.</param>
    public PublicMediaController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Faz stream do arquivo quando o token HMAC, o tenant e a chave são válidos.
    /// </summary>
    /// <param name="token">Token opaco emitido pelo resolvedor de URL de publicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Stream binário ou erro opaco.</returns>
    [HttpGet("{token}")]
    [EndpointSummary("Leitura pública de mídia orgânica por token HMAC")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetMedia(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPublicOrganicMediaStreamQuery(token), cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Code == StorageErrors.KeyInvalid.Code)
            {
                return UnprocessableEntity(
                    Result.Failure(Error.Validation(result.Error.Code, result.Error.Description)));
            }

            return NotFound(Result.Failure(SocialErrors.PublicMediaNotFound));
        }

        return File(result.Value.Content, result.Value.ContentType);
    }
}