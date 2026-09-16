using Analytics.Application.Currencies.Dtos;
using Analytics.Application.Currencies.Queries.ConvertCurrency;
using Analytics.Application.Currencies.Queries.ConvertCurrencyBatch;
using Analytics.Application.Taxonomy.Dtos;
using Analytics.Application.Taxonomy.Queries.BatchClassifyTaxonomy;
using Analytics.Application.Taxonomy.Queries.ClassifyTaxonomy;
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
}
