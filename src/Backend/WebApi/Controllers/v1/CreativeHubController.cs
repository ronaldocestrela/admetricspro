using Analytics.Application.Creatives.DTOs;
using Analytics.Application.Creatives.Queries.GetCreativeFatigueAnalysis;
using Analytics.Application.Creatives.Queries.GetCreativeHubOverview;
using Analytics.Application.Creatives.Queries.GetCrossPlatformComparison;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo Creative Hub e detecção preditiva de fadiga de anúncios criativos (Subfase 5.2).
/// Permite monitorar saturação de audiência, quedas de CTR e comparar eficiência entre Meta Ads e TikTok Ads.
/// </summary>
[ApiController]
[Route("api/v1/analytics/creatives")]
public sealed class CreativeHubController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreativeHubController"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas (MediatR).</param>
    public CreativeHubController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Recupera a visão geral de criativos do workspace com indicadores de fadiga e contadores analíticos consolidados.
    /// </summary>
    /// <param name="workspaceId">Identificador único do workspace.</param>
    /// <param name="startDate">Data inicial opcional para a janela de 7 dias.</param>
    /// <param name="endDate">Data final opcional para a janela de 7 dias.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Objeto de visão geral contendo lista de criativos e resumo estatístico.</returns>
    [HttpGet("overview")]
    [EndpointSummary("Recupera a visão geral de criativos do workspace com indicadores de fadiga")]
    [ProducesResponseType(typeof(Result<CreativeHubOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CreativeHubOverviewDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<CreativeHubOverviewDto>>> GetOverview(
        [FromQuery] Guid workspaceId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCreativeHubOverviewQuery(workspaceId, startDate, endDate);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Analisa detalhadamente a fadiga de um criativo específico através da tendência de CTR e saturação de frequência.
    /// </summary>
    /// <param name="workspaceId">Identificador único do workspace.</param>
    /// <param name="adId">Identificador único do anúncio.</param>
    /// <param name="startDate">Data inicial opcional da série temporal.</param>
    /// <param name="endDate">Data final opcional da série temporal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Diagnóstico de fadiga com recomendação acionável de substituição.</returns>
    [HttpGet("fatigue")]
    [EndpointSummary("Diagnostica a fadiga e saturação de um criativo publicitário")]
    [ProducesResponseType(typeof(Result<CreativeFatigueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CreativeFatigueDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CreativeFatigueDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<CreativeFatigueDto>>> GetFatigueAnalysis(
        [FromQuery] Guid workspaceId,
        [FromQuery] Guid adId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCreativeFatigueAnalysisQuery(workspaceId, adId, startDate, endDate);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Compara o desempenho do mesmo ativo publicitário entre o Meta Ads e o TikTok Ads.
    /// </summary>
    /// <param name="workspaceId">Identificador único do workspace.</param>
    /// <param name="assetFingerprint">Hash de identificação da mídia / preview do criativo.</param>
    /// <param name="assetName">Nome amigável da peça.</param>
    /// <param name="previewUrl">URL opcional de preview.</param>
    /// <param name="startDate">Data inicial da janela de comparação.</param>
    /// <param name="endDate">Data final da janela de comparação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Relatório comparativo de métricas e recomendação de canal mais eficiente.</returns>
    [HttpGet("comparison")]
    [EndpointSummary("Compara a eficiência da mesma peça publicitária no Meta Ads vs. TikTok Ads")]
    [ProducesResponseType(typeof(Result<CrossPlatformComparisonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CrossPlatformComparisonDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<CrossPlatformComparisonDto>>> GetCrossPlatformComparison(
        [FromQuery] Guid workspaceId,
        [FromQuery] string assetFingerprint,
        [FromQuery] string? assetName,
        [FromQuery] string? previewUrl,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCrossPlatformComparisonQuery(workspaceId, assetFingerprint, assetName, previewUrl, startDate, endDate);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
