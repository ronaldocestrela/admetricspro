using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SocialPublishing.Infrastructure.Persistence;

/// <summary>
/// Fábrica de design-time para gerar migrações EF do schema social sem provisionar tenant.
/// </summary>
public sealed class SocialPublishingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SocialPublishingDbContext>
{
    /// <inheritdoc />
    public SocialPublishingDbContext CreateDbContext(string[] args) => new(
        SocialPublishingDbContext.CreateOptions(
            "Server=localhost;Database=AdMetricsPro_SocialDesignOnly;Trusted_Connection=True;TrustServerCertificate=True"));
}
