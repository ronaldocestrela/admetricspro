using Analytics.Application.Creatives.DTOs;
using Analytics.Domain.Creatives;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Analytics.Application.Creatives.Queries.GetCrossPlatformComparison;

/// <summary>
/// Consulta comparativa cross-platform (Meta Ads vs. TikTok Ads) para uma peça ou ativo de mídia.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="AssetFingerprint">Identificador canônico ou hash de mídia da peça.</param>
/// <param name="AssetName">Nome opcional de exibição da peça.</param>
/// <param name="PreviewUrl">URL opcional de preview.</param>
/// <param name="StartDateUtc">Data inicial opcional da janela comparativa.</param>
/// <param name="EndDateUtc">Data final opcional da janela comparativa.</param>
public sealed record GetCrossPlatformComparisonQuery(
    Guid WorkspaceId,
    string AssetFingerprint,
    string? AssetName = null,
    string? PreviewUrl = null,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null) : IRequest<Result<CrossPlatformComparisonDto>>;

/// <summary>
/// Manipulador da consulta comparativa cross-platform.
/// </summary>
public sealed class GetCrossPlatformComparisonQueryHandler : IRequestHandler<GetCrossPlatformComparisonQuery, Result<CrossPlatformComparisonDto>>
{
    private readonly ICreativeHubDataProvider _dataProvider;
    private readonly ICreativeHubAggregator _aggregator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetCrossPlatformComparisonQueryHandler"/>.
    /// </summary>
    /// <param name="dataProvider">Provedor de dados de criativos.</param>
    /// <param name="aggregator">Agregador comparativo cross-platform.</param>
    public GetCrossPlatformComparisonQueryHandler(
        ICreativeHubDataProvider dataProvider,
        ICreativeHubAggregator aggregator)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
    }

    /// <inheritdoc />
    public async Task<Result<CrossPlatformComparisonDto>> Handle(
        GetCrossPlatformComparisonQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<CrossPlatformComparisonDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do workspace é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.AssetFingerprint))
        {
            return Result<CrossPlatformComparisonDto>.Failure(
                Error.Validation("CreativeComparison.InvalidFingerprint", "O identificador do ativo de mídia é obrigatório."));
        }

        var endDate = request.EndDateUtc?.Date ?? DateTime.UtcNow.Date;
        var startDate = request.StartDateUtc?.Date ?? endDate.AddDays(-29); // Padrão 30 dias para comparação

        var metricsResult = await _dataProvider.GetCrossPlatformMetricsAsync(
            request.WorkspaceId,
            request.AssetFingerprint,
            startDate,
            endDate,
            cancellationToken);

        if (metricsResult.IsFailure)
        {
            return Result<CrossPlatformComparisonDto>.Failure(metricsResult.Error);
        }

        var (metaMetrics, tikTokMetrics) = metricsResult.Value;

        var name = !string.IsNullOrWhiteSpace(request.AssetName)
            ? request.AssetName
            : $"Ativo {request.AssetFingerprint}";

        var comparisonResult = _aggregator.CompareCrossPlatform(
            request.AssetFingerprint,
            name,
            request.PreviewUrl,
            metaMetrics,
            tikTokMetrics);

        if (comparisonResult.IsFailure)
        {
            return Result<CrossPlatformComparisonDto>.Failure(comparisonResult.Error);
        }

        var c = comparisonResult.Value;
        var dto = new CrossPlatformComparisonDto(
            c.AssetFingerprint,
            c.AssetName,
            c.PreviewUrl,
            new CrossPlatformMetricsDto(
                c.MetaMetrics.Platform,
                c.MetaMetrics.AdCount,
                c.MetaMetrics.Spend,
                c.MetaMetrics.Impressions,
                c.MetaMetrics.Clicks,
                c.MetaMetrics.Conversions,
                c.MetaMetrics.ConversionValue,
                c.MetaMetrics.Ctr,
                c.MetaMetrics.Cpc,
                c.MetaMetrics.Cpa,
                c.MetaMetrics.Roas),
            new CrossPlatformMetricsDto(
                c.TikTokMetrics.Platform,
                c.TikTokMetrics.AdCount,
                c.TikTokMetrics.Spend,
                c.TikTokMetrics.Impressions,
                c.TikTokMetrics.Clicks,
                c.TikTokMetrics.Conversions,
                c.TikTokMetrics.ConversionValue,
                c.TikTokMetrics.Ctr,
                c.TikTokMetrics.Cpc,
                c.TikTokMetrics.Cpa,
                c.TikTokMetrics.Roas),
            c.WinningPlatform,
            c.CpaDifferencePercentage,
            c.CtrDifferencePercentage,
            c.EfficiencySummary);

        return Result<CrossPlatformComparisonDto>.Success(dto);
    }
}
