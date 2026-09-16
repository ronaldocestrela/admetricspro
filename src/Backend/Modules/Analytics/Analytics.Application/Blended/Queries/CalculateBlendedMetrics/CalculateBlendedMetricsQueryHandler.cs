using Analytics.Application.Blended.Dtos;
using Analytics.Domain.Blended;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Blended.Queries.CalculateBlendedMetrics;

/// <summary>
/// Manipulador da consulta CQRS responsável pela consolidação de métricas agregadas multi-canal.
/// </summary>
public sealed class CalculateBlendedMetricsQueryHandler : IQueryHandler<CalculateBlendedMetricsQuery, BlendedMetricsDto>
{
    private readonly IBlendedMetricsCalculator _calculator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CalculateBlendedMetricsQueryHandler"/>.
    /// </summary>
    /// <param name="calculator">Calculador de métricas blended.</param>
    public CalculateBlendedMetricsQueryHandler(IBlendedMetricsCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    /// <inheritdoc />
    public async Task<Result<BlendedMetricsDto>> Handle(CalculateBlendedMetricsQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<BlendedMetricsDto>.Failure(
                Error.Validation("Query.Null", "A consulta de métricas blended não pode ser nula."));
        }

        var domainItems = request.Items?
            .Select(i => new BlendedMetricInputItem(
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
            .ToList() ?? new List<BlendedMetricInputItem>();

        var result = await _calculator.CalculateAsync(
            domainItems,
            request.TargetCurrency,
            request.TotalStoreRevenue,
            request.TotalNewCustomers,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<BlendedMetricsDto>.Failure(result.Error);
        }

        var domain = result.Value;

        var channelBreakdowns = domain.ChannelBreakdowns
            .Select(b => new BlendedChannelBreakdownDto(
                b.Platform,
                b.Spend,
                b.SpendSharePercentage,
                b.Impressions,
                b.Clicks,
                b.Conversions,
                b.ConversionValue,
                b.Roas,
                b.Cpa,
                b.Cpc,
                b.Cpm,
                b.Ctr))
            .ToList();

        var dto = new BlendedMetricsDto(
            domain.TargetCurrency,
            domain.TotalSpend,
            domain.TotalConversionValue,
            domain.TotalStoreRevenue,
            domain.TotalImpressions,
            domain.TotalClicks,
            domain.TotalConversions,
            domain.TotalNewCustomers,
            domain.MarketingEfficiencyRatio,
            domain.BlendedRoas,
            domain.BlendedCac,
            domain.BlendedCpa,
            domain.BlendedCpc,
            domain.BlendedCpm,
            domain.BlendedCtr,
            channelBreakdowns);

        return Result<BlendedMetricsDto>.Success(dto);
    }
}
