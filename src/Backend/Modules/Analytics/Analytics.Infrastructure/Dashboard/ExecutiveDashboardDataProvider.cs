using Analytics.Domain.Dashboard;
using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Analytics.Infrastructure.Dashboard;

/// <summary>Obtém métricas persistidas via contrato in-memory com Integrations.</summary>
/// <param name="sender">Mediador dos contratos inter-módulos.</param>
public sealed class ExecutiveDashboardDataProvider(ISender sender) : IExecutiveDashboardDataProvider
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RawMetricPoint>>> GetMetricsAsync(
        Guid? workspaceId, DateTime startDateUtc, DateTime endDateUtc,
        CancellationToken cancellationToken = default, string currency = "BRL")
    {
        var result = await sender.Send(new GetDashboardMetricPointsQuery(workspaceId, startDateUtc, endDateUtc), cancellationToken);
        if (result.IsFailure) return Result<IReadOnlyList<RawMetricPoint>>.Failure(result.Error);
        if (result.Value.Any(m => !string.Equals(m.Currency, currency, StringComparison.OrdinalIgnoreCase)))
            return Result<IReadOnlyList<RawMetricPoint>>.Failure(Error.Validation("Dashboard.CurrencyMismatch",
                "As contas possuem moeda diferente do filtro. Selecione um workspace e sua moeda para visualizar os valores originais."));
        return Result<IReadOnlyList<RawMetricPoint>>.Success(result.Value.Select(m => new RawMetricPoint(
            m.Date, m.Platform switch { "MetaAds" => "Meta", "GoogleAds" => "Google", "TikTokAds" => "TikTok", "BingAds" => "Bing", _ => m.Platform },
            "Unknown", m.Spend, m.Revenue, m.Impressions, m.Clicks, m.Conversions)).ToArray());
    }
}
