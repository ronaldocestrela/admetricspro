using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>Upload e processamento de mídias da conta Meta vinculada.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Tenant")]
[ApiController]
[Route("api/v1/integrations/meta/media")]
public sealed class MetaMediaController(ISender sender) : ControllerBase
{
    /// <summary>Envia imagem ou vídeo sem publicar ou ativar anúncios.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna vinculada.</param>
    /// <param name="operationId">Chave idempotente do upload.</param>
    /// <param name="file">PNG/JPEG de até 10 MB ou MP4 de até 100 MB.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Hash da imagem ou ID do vídeo em processamento.</returns>
    [HttpPost]
    [RequestSizeLimit(110_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 110_000_000)]
    [EndpointSummary("Envia mídia à conta Meta com auditoria e idempotência")]
    [ProducesResponseType(typeof(Result<MetaMediaResult>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)] [ProducesResponseType(409)] [ProducesResponseType(413)] [ProducesResponseType(422)]
    public async Task<IActionResult> Upload([FromForm] Guid workspaceId, [FromForm] Guid accountId,
        [FromForm] Guid operationId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is < 1 or > 100_000_000 || (file.ContentType.StartsWith("image/", StringComparison.Ordinal) && file.Length > 10_000_000))
            return StatusCode(413, Result.Failure(Error.Validation("Meta.MediaTooLarge", "Tamanho de mídia inválido.")));
        using var content = new MemoryStream((int)file.Length);
        await file.CopyToAsync(content, cancellationToken);
        var result = await sender.Send(new UploadMetaMediaCommand(operationId, workspaceId, accountId, file.ContentType, Path.GetFileName(file.FileName), content.ToArray()), cancellationToken);
        return Respond(result);
    }

    /// <summary>Consulta estado de um vídeo enviado por esta conta e workspace.</summary>
    /// <param name="operationId">Operação de upload do vídeo.</param>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Estado oficial do vídeo.</returns>
    [HttpGet("videos/{operationId:guid}")]
    [EndpointSummary("Consulta processamento de vídeo Meta enviado pelo workspace")]
    [ProducesResponseType(typeof(Result<MetaMediaResult>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)]
    public async Task<IActionResult> Video(Guid operationId, [FromQuery] Guid workspaceId, [FromQuery] Guid accountId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetMetaVideoStatusQuery(workspaceId, accountId, operationId), cancellationToken));

    private IActionResult Respond<T>(Result<T> result) => result.IsSuccess ? Ok(result) : StatusCode(result.Error.Type switch
    { ErrorType.Unauthorized => 401, ErrorType.Forbidden => 403, ErrorType.NotFound => 404, ErrorType.Conflict => 409, ErrorType.Validation => 422, _ => 502 }, result);
}
