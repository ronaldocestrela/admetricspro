using Analytics.Domain.Blended;
using Analytics.Domain.Currencies;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Infrastructure.Blended;

/// <summary>
/// Implementação de alto desempenho para consolidação e cálculo de métricas agregadas multi-canal,
/// incluindo MER (Marketing Efficiency Ratio), Blended ROAS, Blended CAC e detalhamento por plataforma.
/// </summary>
public sealed class BlendedMetricsCalculator : IBlendedMetricsCalculator
{
    private readonly ICurrencyConverter _currencyConverter;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BlendedMetricsCalculator"/>.
    /// </summary>
    /// <param name="currencyConverter">Conversor cambial resiliente para normalização monetária.</param>
    public BlendedMetricsCalculator(ICurrencyConverter currencyConverter)
    {
        _currencyConverter = currencyConverter ?? throw new ArgumentNullException(nameof(currencyConverter));
    }

    /// <inheritdoc />
    public async Task<Result<BlendedMetricsResult>> CalculateAsync(
        IEnumerable<BlendedMetricInputItem> metrics,
        string targetCurrency,
        decimal? totalStoreRevenue = null,
        int? totalNewCustomers = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetCurrency) || targetCurrency.Trim().Length != 3)
        {
            return Result<BlendedMetricsResult>.Failure(
                Error.Validation("BlendedMetrics.InvalidCurrency", "A moeda de destino deve ser um código ISO válido de 3 caracteres."));
        }

        var normalizedTargetCurrency = targetCurrency.Trim().ToUpperInvariant();

        if (totalStoreRevenue.HasValue && totalStoreRevenue.Value < 0)
        {
            return Result<BlendedMetricsResult>.Failure(
                Error.Validation("BlendedMetrics.InvalidAmount", "A receita total da loja não pode ser negativa."));
        }

        if (totalNewCustomers.HasValue && totalNewCustomers.Value < 0)
        {
            return Result<BlendedMetricsResult>.Failure(
                Error.Validation("BlendedMetrics.InvalidNewCustomers", "O número total de novos clientes não pode ser negativo."));
        }

        var itemsList = metrics?.ToList() ?? new List<BlendedMetricInputItem>();

        // Validação de valores negativos
        foreach (var item in itemsList)
        {
            if (item.Spend < 0 || item.ConversionValue < 0 || item.Impressions < 0 || item.Clicks < 0 || item.Conversions < 0)
            {
                return Result<BlendedMetricsResult>.Failure(
                    Error.Validation("BlendedMetrics.InvalidAmount", "Os valores de métricas (Spend, ConversionValue, impressões, cliques, conversões) não podem ser negativos."));
            }

            if (item.NewCustomers.HasValue && item.NewCustomers.Value < 0)
            {
                return Result<BlendedMetricsResult>.Failure(
                    Error.Validation("BlendedMetrics.InvalidNewCustomers", "Novos clientes não podem ser negativos."));
            }
        }

        // Conversão cambial normalizada para cada item
        var convertedItems = new List<ConvertedMetricItem>(itemsList.Count);

        foreach (var item in itemsList)
        {
            var itemSourceCurrency = string.IsNullOrWhiteSpace(item.Currency) ? "BRL" : item.Currency.Trim().ToUpperInvariant();

            decimal convertedSpend = item.Spend;
            decimal convertedConversionValue = item.ConversionValue;

            if (itemSourceCurrency != normalizedTargetCurrency)
            {
                var spendConv = await _currencyConverter.ConvertAsync(
                    item.Spend,
                    itemSourceCurrency,
                    normalizedTargetCurrency,
                    item.Date,
                    cancellationToken);

                if (spendConv.IsFailure)
                {
                    return Result<BlendedMetricsResult>.Failure(spendConv.Error);
                }

                convertedSpend = spendConv.Value.ConvertedAmount;

                var valueConv = await _currencyConverter.ConvertAsync(
                    item.ConversionValue,
                    itemSourceCurrency,
                    normalizedTargetCurrency,
                    item.Date,
                    cancellationToken);

                if (valueConv.IsFailure)
                {
                    return Result<BlendedMetricsResult>.Failure(valueConv.Error);
                }

                convertedConversionValue = valueConv.Value.ConvertedAmount;
            }

            convertedItems.Add(new ConvertedMetricItem(
                item.Platform,
                item.CampaignId,
                item.ExternalCampaignId,
                item.Date,
                convertedSpend,
                convertedConversionValue,
                item.Impressions,
                item.Clicks,
                item.Conversions,
                item.NewCustomers));
        }

        // Totais Agregados
        decimal totalSpend = convertedItems.Sum(i => i.Spend);
        decimal totalConversionValue = convertedItems.Sum(i => i.ConversionValue);
        long totalImpressions = convertedItems.Sum(i => i.Impressions);
        long totalClicks = convertedItems.Sum(i => i.Clicks);
        decimal totalConversions = convertedItems.Sum(i => i.Conversions);

        int totalAcquiredCustomers = totalNewCustomers ?? convertedItems.Sum(i => i.NewCustomers ?? 0);

        // Fórmulas Financeiras Consolidadas com tratamento contra divisão por zero
        decimal revenueForMer = totalStoreRevenue ?? totalConversionValue;
        decimal mer = totalSpend > 0 ? revenueForMer / totalSpend : 0m;
        decimal blendedRoas = totalSpend > 0 ? totalConversionValue / totalSpend : 0m;
        decimal blendedCac = totalAcquiredCustomers > 0 ? totalSpend / totalAcquiredCustomers : 0m;
        decimal blendedCpa = totalConversions > 0 ? totalSpend / totalConversions : 0m;
        decimal blendedCpc = totalClicks > 0 ? totalSpend / totalClicks : 0m;
        decimal blendedCpm = totalImpressions > 0 ? (totalSpend / totalImpressions) * 1000m : 0m;
        decimal blendedCtr = totalImpressions > 0 ? ((decimal)totalClicks / totalImpressions) * 100m : 0m;

        // Quebra e participação por plataforma (Channel Breakdowns)
        var channelGroups = convertedItems
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Platform) ? "Other" : i.Platform.Trim())
            .ToList();

        var channelBreakdowns = new List<BlendedChannelBreakdown>();

        foreach (var group in channelGroups)
        {
            string platformName = group.Key;
            decimal platformSpend = group.Sum(i => i.Spend);
            decimal platformConversionValue = group.Sum(i => i.ConversionValue);
            long platformImpressions = group.Sum(i => i.Impressions);
            long platformClicks = group.Sum(i => i.Clicks);
            decimal platformConversions = group.Sum(i => i.Conversions);

            decimal spendSharePercentage = totalSpend > 0 ? (platformSpend / totalSpend) * 100m : 0m;
            decimal platformRoas = platformSpend > 0 ? platformConversionValue / platformSpend : 0m;
            decimal platformCpa = platformConversions > 0 ? platformSpend / platformConversions : 0m;
            decimal platformCpc = platformClicks > 0 ? platformSpend / platformClicks : 0m;
            decimal platformCpm = platformImpressions > 0 ? (platformSpend / platformImpressions) * 1000m : 0m;
            decimal platformCtr = platformImpressions > 0 ? ((decimal)platformClicks / platformImpressions) * 100m : 0m;

            channelBreakdowns.Add(new BlendedChannelBreakdown(
                platformName,
                platformSpend,
                spendSharePercentage,
                platformImpressions,
                platformClicks,
                platformConversions,
                platformConversionValue,
                platformRoas,
                platformCpa,
                platformCpc,
                platformCpm,
                platformCtr));
        }

        var result = new BlendedMetricsResult(
            normalizedTargetCurrency,
            totalSpend,
            totalConversionValue,
            totalStoreRevenue,
            totalImpressions,
            totalClicks,
            totalConversions,
            totalAcquiredCustomers,
            mer,
            blendedRoas,
            blendedCac,
            blendedCpa,
            blendedCpc,
            blendedCpm,
            blendedCtr,
            channelBreakdowns);

        return Result<BlendedMetricsResult>.Success(result);
    }

    private sealed record ConvertedMetricItem(
        string Platform,
        Guid? CampaignId,
        string? ExternalCampaignId,
        DateTime Date,
        decimal Spend,
        decimal ConversionValue,
        long Impressions,
        long Clicks,
        decimal Conversions,
        int? NewCustomers);
}
