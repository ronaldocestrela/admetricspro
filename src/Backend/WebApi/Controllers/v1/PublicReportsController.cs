using Analytics.Application.Reports.DTOs;
using Analytics.Application.Reports.Queries;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador público para acesso e download de relatórios white-label via token de compartilhamento.
/// Não exige autenticação de agência.
/// </summary>
[ApiController]
[Route("api/v1/public/reports")]
[AllowAnonymous]
public sealed class PublicReportsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PublicReportsController"/>.
    /// </summary>
    public PublicReportsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Obtém os dados consolidados do relatório e branding da agência a partir de um token de compartilhamento.
    /// </summary>
    /// <param name="token">Token alfanumérico seguro gerado para o relatório.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados consolidados do relatório white-label.</returns>
    [HttpGet("{token}")]
    [EndpointSummary("Consulta pública de relatório interativo por token de compartilhamento")]
    [ProducesResponseType(typeof(Result<PublicSharedReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PublicSharedReportDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<PublicSharedReportDto>>> GetSharedReport(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPublicSharedReportQuery(token), cancellationToken);
        if (result.IsFailure) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Baixa diretamente o PDF executivo white-label correspondente ao token de compartilhamento.
    /// </summary>
    /// <param name="token">Token alfanumérico seguro do relatório.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Arquivo PDF para download.</returns>
    [HttpGet("{token}/pdf")]
    [EndpointSummary("Download público do PDF de um relatório por token de compartilhamento")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadSharedReportPdf(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetReportPdfQuery(null, token), cancellationToken);
        if (result.IsFailure) return NotFound(result);

        return File(result.Value, "application/pdf", $"relatorio-executivo-{token[..8]}.pdf");
    }
}
