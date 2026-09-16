using Analytics.Application.Attribution.Dtos;
using Analytics.Application.Attribution.Queries.CalculateAttribution;
using Analytics.Application.Blended.Dtos;
using Analytics.Application.Blended.Queries.CalculateBlendedMetrics;
using Analytics.Application.Currencies.Dtos;
using Analytics.Application.Currencies.Queries.ConvertCurrency;
using Analytics.Application.Currencies.Queries.ConvertCurrencyBatch;
using Analytics.Application.Dashboard.DTOs;
using Analytics.Application.Dashboard.Queries.GetExecutiveDashboard;
using Analytics.Application.Taxonomy.Dtos;
using Analytics.Application.Taxonomy.Queries.BatchClassifyTaxonomy;
using Analytics.Application.Taxonomy.Queries.ClassifyTaxonomy;
using Analytics.Domain.Attribution;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelas funcionalidades analíticas de inteligência de performance,
/// incluindo normalização cambial multi-moeda e classificação taxonômica automatizada de tráfego pago.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AnalyticsController"/>.
    /// </summary>
    /// <param name="sender">Mediador CQRS.</param>
    public AnalyticsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Realiza a conversão monetária pontual entre moedas suportadas com base na cotação da data indicada.
    /// </summary>
    /// <param name="amount">Valor a ser convertido.</param>
    /// <param name="sourceCurrency">Moeda original (ex: USD, EUR).</param>
    /// <param name="targetCurrency">Moeda de destino (ex: BRL).</param>
    /// <param name="date">Data da cotação de referência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com valor convertido e taxa aplicada.</returns>
    [HttpGet("currency/convert")]
    [EndpointSummary("Converte montante monetário pela cotação do dia")]
    [ProducesResponseType(typeof(Result<CurrencyConversionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CurrencyConversionDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<CurrencyConversionDto>>> ConvertCurrency(
        [FromQuery] decimal amount,
        [FromQuery] string sourceCurrency,
        [FromQuery] string targetCurrency,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = date ?? DateTime.UtcNow;
        var query = new ConvertCurrencyQuery(amount, sourceCurrency, targetCurrency, effectiveDate);

        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Converte em lote múltiplos valores monetários heterogêneos para uma moeda de destino única.
    /// </summary>
    /// <param name="request">Lista de valores, moedas e datas, além da moeda alvo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de valores normalizados na moeda solicitada.</returns>
    [HttpPost("currency/convert-batch")]
    [EndpointSummary("Converte em lote múltiplos valores monetários para moeda unificada")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<CurrencyConversionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<CurrencyConversionDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<IReadOnlyList<CurrencyConversionDto>>>> ConvertCurrencyBatch(
        [FromBody] ConvertCurrencyBatchApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<IReadOnlyList<CurrencyConversionDto>>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var items = request.Items?
            .Select(i => new CurrencyBatchItemInput(i.Amount, i.SourceCurrency, i.Date))
            .ToList() ?? new List<CurrencyBatchItemInput>();

        var query = new ConvertCurrencyBatchQuery(items, request.TargetCurrency);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Realiza a classificação taxonômica automatizada de uma campanha ou criativo a partir de sua nomenclatura.
    /// </summary>
    /// <param name="request">Nomenclaturas da campanha, conjunto e anúncio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Estágio de funil, tipo de audiência, formato de criativo e tags extraídas.</returns>
    [HttpPost("taxonomy/classify")]
    [EndpointSummary("Classifica automaticamente estágio de funil, público e formato")]
    [ProducesResponseType(typeof(Result<TaxonomyClassificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TaxonomyClassificationDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<TaxonomyClassificationDto>>> ClassifyTaxonomy(
        [FromBody] ClassifyTaxonomyApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<TaxonomyClassificationDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var query = new ClassifyTaxonomyQuery(
            request.CampaignName,
            request.AdSetName,
            request.AdName,
            request.ReferenceId);

        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Realiza a classificação taxonômica automatizada em lote de múltiplas campanhas e criativos simultaneamente.
    /// </summary>
    /// <param name="request">Lista de itens de campanha com nomenclaturas a classificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de classificações taxonômicas enriquecidas.</returns>
    [HttpPost("taxonomy/classify-batch")]
    [EndpointSummary("Classifica em lote taxonomias de múltiplas campanhas e anúncios")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<TaxonomyClassificationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<TaxonomyClassificationDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<IReadOnlyList<TaxonomyClassificationDto>>>> ClassifyTaxonomyBatch(
        [FromBody] BatchClassifyTaxonomyApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<IReadOnlyList<TaxonomyClassificationDto>>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var items = request.Items?
            .Select(i => new BatchClassifyTaxonomyItem(i.CampaignName, i.AdSetName, i.AdName, i.ReferenceId))
            .ToList() ?? new List<BatchClassifyTaxonomyItem>();

        var query = new BatchClassifyTaxonomyQuery(items);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Calcula e consolida métricas agregadas multi-canal (MER, Blended ROAS, Blended CAC, CPA, CPC, CPM, CTR)
    /// com suporte a conversão cambial multi-moeda e quebra percentual de investimento por plataforma.
    /// </summary>
    /// <param name="request">Parâmetros contendo métricas por rede, moeda alvo, receita de loja e novos clientes.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo todos os indicadores blended calculados.</returns>
    [HttpPost("blended-metrics")]
    [EndpointSummary("Calcula métricas agregadas multi-canal (MER, Blended ROAS, Blended CAC)")]
    [ProducesResponseType(typeof(Result<BlendedMetricsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<BlendedMetricsDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<BlendedMetricsDto>>> CalculateBlendedMetrics(
        [FromBody] CalculateBlendedMetricsApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<BlendedMetricsDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var items = request.Items?
            .Select(i => new BlendedMetricItemInput(
                i.Platform,
                i.CampaignId,
                i.ExternalCampaignId,
                i.Date,
                i.Spend,
                i.Currency,
                i.Impressions,
                i.Clicks,
                i.Conversions,
                i.ConversionValue,
                i.NewCustomers))
            .ToList() ?? new List<BlendedMetricItemInput>();

        var query = new CalculateBlendedMetricsQuery(
            items,
            request.TargetCurrency ?? "BRL",
            request.TotalStoreRevenue,
            request.TotalNewCustomers);

        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Processa jornadas de conversão e gera relatório analítico comparando os modelos de atribuição
    /// Primeiro Clique (First-Touch), Último Clique (Last-Touch) e Linear, incluindo tráfego assistido.
    /// </summary>
    /// <param name="request">Lista de jornadas e touchpoints dos usuários e custos por canal opcionais.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado comparativo side-by-side entre os modelos de atribuição.</returns>
    [HttpPost("attribution")]
    [EndpointSummary("Calcula e compara modelos de atribuição multi-canal (First, Last, Linear)")]
    [ProducesResponseType(typeof(Result<AttributionComparisonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AttributionComparisonDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<AttributionComparisonDto>>> CalculateAttribution(
        [FromBody] CalculateAttributionApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<AttributionComparisonDto>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var journeys = request.Journeys?
            .Select(j => new ConversionJourneyInput(
                j.JourneyId,
                j.CustomerId,
                j.ConvertedAtUtc,
                j.ConversionValue,
                j.Touchpoints?
                    .Select(tp => new AttributionTouchpointInput(
                        tp.Channel,
                        tp.CampaignName,
                        tp.OccurredAtUtc,
                        tp.TouchType == 2 ? TouchpointType.Impression : TouchpointType.Click,
                        tp.Cost))
                    .ToList() ?? new List<AttributionTouchpointInput>()))
            .ToList() ?? new List<ConversionJourneyInput>();

        AttributionModelType? modelType = request.ModelType switch
        {
            1 => AttributionModelType.FirstTouch,
            2 => AttributionModelType.LastTouch,
            3 => AttributionModelType.Linear,
            _ => null
        };

        var query = new CalculateAttributionQuery(journeys, modelType, request.ChannelCosts);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtém o resumo executivo unificado cross-network (Spend, CPC, CPM, CTR, CPA, ROAS) com comparação
    /// automática com o período anterior equivalente, série temporal diária e quebra por plataforma e dispositivo.
    /// </summary>
    /// <param name="workspaceId">Identificador opcional do workspace para filtragem de portfólio.</param>
    /// <param name="startDateUtc">Data inicial UTC do período corrente (padrão: D-7).</param>
    /// <param name="endDateUtc">Data final UTC do período corrente (padrão: agora).</param>
    /// <param name="platform">Filtro opcional por canal (Meta, Google, TikTok, Bing).</param>
    /// <param name="device">Filtro opcional por dispositivo (Mobile, Desktop, Tablet).</param>
    /// <param name="currency">Moeda monetária padrão da visualização (padrão: BRL).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resumo executivo consolidado com comparação temporal.</returns>
    [HttpGet("dashboard/executive")]
    [EndpointSummary("Obtém resumo executivo unificado cross-network com comparação temporal e deltas")]
    [ProducesResponseType(typeof(Result<ExecutiveDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ExecutiveDashboardDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<ExecutiveDashboardDto>>> GetExecutiveDashboard(
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] DateTime? startDateUtc = null,
        [FromQuery] DateTime? endDateUtc = null,
        [FromQuery] string? platform = null,
        [FromQuery] string? device = null,
        [FromQuery] string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetExecutiveDashboardQuery(
            workspaceId,
            startDateUtc,
            endDateUtc,
            platform,
            device,
            currency);

        var result = await _sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
