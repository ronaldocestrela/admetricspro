using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Campaigns.Queries.GetCampaignHierarchy;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão estrutural de campanhas, conjuntos de anúncios e criativos sincronizados
/// das plataformas de mídia paga (Meta Ads, Google Ads, TikTok Ads e Bing Ads).
/// </summary>
[ApiController]
[Route("api/v1/integrations/[controller]")]
public sealed class CampaignsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CampaignsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory.</param>
    public CampaignsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Executa a sincronização paginada da hierarquia estrutural de campanhas, conjuntos e anúncios.
    /// </summary>
    /// <param name="request">Filtros de workspace, conta conectada e plataforma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resumo executivo com totalizadores de entidades sincronizadas.</returns>
    [HttpPost("sync")]
    [EndpointSummary("Dispara a sincronização estrutural de campanhas de um workspace")]
    [ProducesResponseType(typeof(Result<SyncCampaignHierarchySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SyncCampaignHierarchySummaryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<SyncCampaignHierarchySummaryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<SyncCampaignHierarchySummaryDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<SyncCampaignHierarchySummaryDto>>> SyncCampaigns(
        [FromBody] SyncCampaignHierarchyApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<SyncCampaignHierarchySummaryDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new SyncCampaignHierarchyCommand(
            request.WorkspaceId,
            request.ConnectedAdAccountId,
            request.Platform);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Unauthorized => Unauthorized(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Consulta a hierarquia estrutural de campanhas, conjuntos de anúncios e criativos normalizados.
    /// </summary>
    /// <param name="workspaceId">Identificador obrigatório do workspace.</param>
    /// <param name="connectedAdAccountId">Filtro opcional por conta de anúncios.</param>
    /// <param name="campaignId">Filtro opcional por campanha específica.</param>
    /// <param name="platform">Filtro opcional por plataforma.</param>
    /// <param name="status">Filtro opcional por status.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista hierárquica completa de campanhas.</returns>
    [HttpGet]
    [EndpointSummary("Obtém a estrutura completa de campanhas, conjuntos e anúncios de um workspace")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<CampaignHierarchyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<CampaignHierarchyDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<IReadOnlyList<CampaignHierarchyDto>>>> GetCampaigns(
        [FromQuery] Guid workspaceId,
        [FromQuery] Guid? connectedAdAccountId = null,
        [FromQuery] Guid? campaignId = null,
        [FromQuery] string? platform = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCampaignHierarchyQuery(
            workspaceId,
            connectedAdAccountId,
            campaignId,
            platform,
            status);

        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Executa operações em massa (ativar, pausar ou reajustar orçamento) em múltiplas campanhas com tolerância a falhas parciais.
    /// </summary>
    /// <param name="request">Payload contendo o identificador do workspace e a lista de operações a serem aplicadas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado consolidado informando itens alterados com sucesso e itens que falharam.</returns>
    [HttpPost("bulk")]
    [EndpointSummary("Executa operações em lote (ativar, pausar, alterar budget) em múltiplas campanhas")]
    [ProducesResponseType(typeof(Result<BulkCampaignOperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<BulkCampaignOperationResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<BulkCampaignOperationResultDto>>> ExecuteBulkOperations(
        [FromBody] BulkCampaignOperationApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<BulkCampaignOperationResultDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var operations = request.Operations?
            .Select(o => new BulkCampaignOperationItem(
                o.CampaignId,
                o.Action,
                o.DailyBudget,
                o.PercentageChange,
                o.Reason))
            .ToList() ?? [];

        var command = new BulkCampaignOperationCommand(request.WorkspaceId, operations);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
