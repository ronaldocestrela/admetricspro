using Microsoft.EntityFrameworkCore.Design;

namespace Analytics.Infrastructure.Persistence;

/// <summary>
/// Fábrica de design-time para gerar migrações EF do schema analytics sem provisionar tenant.
/// </summary>
public sealed class AnalyticsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    /// <inheritdoc />
    public AnalyticsDbContext CreateDbContext(string[] args) => new(
        AnalyticsDbContext.CreateOptions(
            "Server=localhost;Database=AdMetricsPro_AnalyticsDesignOnly;Trusted_Connection=True;TrustServerCertificate=True"));
}
