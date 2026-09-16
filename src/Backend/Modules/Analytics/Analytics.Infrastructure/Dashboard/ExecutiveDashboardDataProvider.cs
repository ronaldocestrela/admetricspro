using Analytics.Domain.Dashboard;

namespace Analytics.Infrastructure.Dashboard;

/// <summary>
/// Provedor de dados em memória e de demonstração para o dashboard executivo analítico.
/// </summary>
public sealed class ExecutiveDashboardDataProvider : IExecutiveDashboardDataProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyList<RawMetricPoint>> GetMetricsAsync(
        Guid? workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        // Se workspace for nulo ou vazio, retorna lista vazia (Empty State)
        if (workspaceId == null || workspaceId == Guid.Empty)
        {
            return Task.FromResult<IReadOnlyList<RawMetricPoint>>(Array.Empty<RawMetricPoint>());
        }

        // Se o workspace possuir dados demonstrativos ou dados provisionados, constrói série normalizada
        var days = (int)(endDateUtc.Date - startDateUtc.Date).TotalDays + 1;
        if (days <= 0)
        {
            return Task.FromResult<IReadOnlyList<RawMetricPoint>>(Array.Empty<RawMetricPoint>());
        }

        var platforms = new[] { "Meta", "Google", "TikTok", "Bing" };
        var devices = new[] { "Mobile", "Desktop", "Tablet" };
        var random = new Random(workspaceId.Value.GetHashCode());

        var points = new List<RawMetricPoint>();
        for (int i = 0; i < days; i++)
        {
            var date = startDateUtc.Date.AddDays(i);
            foreach (var platform in platforms)
            {
                foreach (var device in devices)
                {
                    // Fator proporcional de volume
                    var baseSpend = platform switch
                    {
                        "Meta" => 150m + random.Next(10, 50),
                        "Google" => 120m + random.Next(10, 40),
                        "TikTok" => 60m + random.Next(5, 20),
                        _ => 30m + random.Next(5, 15)
                    };

                    if (device == "Mobile") baseSpend *= 1.8m;
                    else if (device == "Tablet") baseSpend *= 0.3m;

                    var clicks = (long)(baseSpend * (platform == "Google" ? 0.8m : 1.4m));
                    var impressions = clicks * (platform == "TikTok" ? 80 : 40);
                    var conversions = Math.Round((decimal)clicks * 0.04m, 2);
                    var revenue = conversions * (platform == "Google" ? 180m : 120m);

                    points.Add(new RawMetricPoint(
                        date,
                        platform,
                        device,
                        Math.Round(baseSpend, 2),
                        Math.Round(revenue, 2),
                        impressions,
                        clicks,
                        conversions));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<RawMetricPoint>>(points);
    }
}
